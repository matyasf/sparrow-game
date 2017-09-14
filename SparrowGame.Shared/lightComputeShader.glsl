#version 310 es
//#version 450
// what if a light source comes in from outside of the map? -- it needs much bigger tex size (~1500x1500) and just a small part is visible
#define MAX_NUM_TOTAL_LIGHTS 20            
#define LOCAL_WG_SIZE 128u
            
layout (local_size_x = LOCAL_WG_SIZE, local_size_y = 1) in; // product of all must be max 128. Local size of the shader
layout (rgba8, binding = 0) uniform restrict highp image2D img_output;
layout (rgba8, binding = 1) uniform readonly highp image2D colorTex; // determines color
layout (rgba8, binding = 2) uniform readonly highp image2D transpTex; // determines transparency

struct LightData {
    vec4 lightColor; // light color and alpha
    ivec2 lightPos; // light pos
};

layout (std140, binding = 3) uniform Lights {
    LightData light[MAX_NUM_TOTAL_LIGHTS];
};

uniform uint lightNum;

void main() {
    uint global_coords = gl_WorkGroupID.x; // postion in global work group; 0 = left, 1 = right, 2 = top, 3 = bottom
    uint global_coords2 = gl_WorkGroupID.y; // position in second global wg, determines which segment to render (0..3)
    uint local_coords = gl_LocalInvocationID.x; // * 0.5f; // get position in local work group (0...127)
    vec2 bgSize =  vec2(imageSize(colorTex));
    const int txrsiz = 512; 
    // determine coordinates where rendering ends
    ivec2 endPoint = ivec2(0, 0);
    /*
    if (global_coords < 2u) {// on the left or right
        endPoint.y = int(local_coords + global_coords2 * 128u);
        if (global_coords == 1u) { // right
            endPoint.x = txrsiz;
        }
    }
    else { // on the top or bottom
        endPoint.x = int(local_coords + global_coords2 * 128u);
        if (global_coords == 3u) {
            endPoint.y = txrsiz;
        }
    }
    */
    // calculate light to the endpoint
    vec4 transpPixel;
    vec4 colorPixel;
    vec4 currentOutPixel;
    ivec2 coords; 
    const float transmit = 0.0014f;// light transmission constant coeficient <0,1>
    float currentAlpha;
   
    for (uint i = 0u; i < lightNum; i++) {
        vec4 lightRay = light[i].lightColor; // initial light color
        int x0 = light[i].lightPos.x;
        int y0 = light[i].lightPos.y;
        
        // draw it only in a circle
        const float PI = 3.1415926535897932384626433832795;
        const float RAY_LENGTH = 256.0f;
        uint renderNodeNum = global_coords * LOCAL_WG_SIZE * 4u + global_coords2 * LOCAL_WG_SIZE + local_coords;
        endPoint.x = int(RAY_LENGTH * sin(float(renderNodeNum) * PI / 1024.0f)) + x0;
        endPoint.x = endPoint.x > txrsiz ? txrsiz : endPoint.x;
        endPoint.x = endPoint.x < 0 ? 0 : endPoint.x;
        endPoint.y = int(RAY_LENGTH * cos(float(renderNodeNum) * PI / 1024.0f)) + y0;
        endPoint.y = endPoint.y > txrsiz ? txrsiz : endPoint.y;
        endPoint.y = endPoint.y < 0 ? 0 : endPoint.y;
        
        
        // Bresenham's line algorithm
        /*
        int x1 = endPoint.x;
        int y1 = endPoint.y;
        int dx = abs(x1 - x0);
        int dy = abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        while (true) {
            coords.x = x0;
            coords.y = y0;
            // calculate transparency
            transpPixel = imageLoad(transpTex, coords);   
            currentAlpha = (transpPixel.b + transpPixel.g * 10.0 + transpPixel.r * 100.0) / 111.0;
            // calculate color
            colorPixel = imageLoad(colorTex, coords);
            lightRay.rgb = min(colorPixel.rgb, lightRay.rgb) - (1.0 - currentAlpha) - transmit;
            currentOutPixel = imageLoad(img_output, coords);
            currentOutPixel.rgb = max(currentOutPixel.rgb, lightRay.rgb);
            currentOutPixel.a = lightRay.a;
            // write color
            imageStore(img_output, coords, currentOutPixel);
            if (currentOutPixel.r + currentOutPixel.g + currentOutPixel.b < 0.05f) break;
            
            if (x0 == x1 && y0 == y1) {
                break;
            }
            int e2 = err * 2;
            if (e2 > -dx) {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx){
                err += dx;
                y0 += sy;
            }
        }
        */
        
        // vector approximation. Works, but has moire artifacts.
        vec2 dt = normalize(vec2(endPoint - light[i].lightPos));
        vec2 t = vec2(light[i].lightPos);
    	for (int k = 0; k < 256; k++) {
            coords.x = int(t.x);
            coords.y = int(t.y);
            
            // calculate transparency
            transpPixel = imageLoad(transpTex, coords);   
            currentAlpha = (transpPixel.b + transpPixel.g * 10.0f + transpPixel.r * 100.0f) / 111.0f;
            // calculate color
            colorPixel = imageLoad(colorTex, coords);
            lightRay.rgb = min(colorPixel.rgb, lightRay.rgb) - (1.0f - currentAlpha) - transmit; 
            currentOutPixel = imageLoad(img_output, coords);
            currentOutPixel.rgb = max(currentOutPixel.rgb, lightRay.rgb);
            currentOutPixel.a = lightRay.a;
            // write color
            imageStore(img_output, coords, currentOutPixel);
            //imageStore(img_output, coords, lightRay);
            
            if (currentOutPixel.r + currentOutPixel.g + currentOutPixel.b < 0.05f) break;
            if (dot(vec2(endPoint) - t, dt) < 0.6f) break;
            t += dt;
    	}

	}
}

