//#version 310 es
#version 450
// what if a light source comes in from outside of the map? -- it needs much bigger tex size (~1500x1500) and just a small part is visible
 
layout (local_size_x = 220, local_size_y = 1, local_size_z = 1) in; // product of all should be max 128. Local size of the shader
layout (rgba8, binding = 0) uniform restrict highp image2D img_output;
layout (rgba8, binding = 1) uniform readonly highp image2D colorTex; // determines color
layout (rgba8, binding = 2) uniform readonly highp image2D transpTex; // determines transparency

uniform ivec2 lightPos;
uniform vec4 lightColor;

void main() {
    //vec2 bgSize =  vec2(imageSize(colorTex));
    const int txrsiz = 512; 
    
    // find thread number and total threads
    const uint NUM_THREADS = gl_NumWorkGroups.x * gl_NumWorkGroups.y * gl_NumWorkGroups.z * gl_WorkGroupSize.x * gl_WorkGroupSize.y * gl_WorkGroupSize.z;
    // thread ID                    
    const uint THREAD_ID = gl_WorkGroupID.x +
                           gl_WorkGroupID.y * gl_NumWorkGroups.x +
                           gl_WorkGroupID.z * gl_NumWorkGroups.x * gl_NumWorkGroups.y +
                           gl_LocalInvocationID.x * gl_NumWorkGroups.x * gl_NumWorkGroups.y * gl_NumWorkGroups.z + 
                           gl_LocalInvocationID.y * gl_NumWorkGroups.x * gl_NumWorkGroups.y * gl_NumWorkGroups.z * gl_WorkGroupSize.x +
                           gl_LocalInvocationID.z * gl_NumWorkGroups.x * gl_NumWorkGroups.y * gl_NumWorkGroups.z * gl_WorkGroupSize.x * gl_WorkGroupSize.y;
        
    vec4 transpPixel;
    vec4 colorPixel;
    vec4 currentOutPixel;
    ivec2 coords; 
    const float TRANSMIT = 0.0045f;// light transmission constant coeficient <0,1>
    float currentAlpha;
        
    vec4 lightRay = lightColor; // initial light color
    
    // draw it only in a circle
    const float PI = 3.1415926535897932384626433832795;
    const int RAY_LENGTH = 250;
    // determine coordinates where rendering ends
    vec2 endPoint = vec2(0, 0);
    endPoint.x = RAY_LENGTH * sin(float(THREAD_ID) * PI / NUM_THREADS * 2) + lightPos.x; // Why is the 2 needed?
    endPoint.x = endPoint.x > txrsiz ? txrsiz : endPoint.x;
    endPoint.x = endPoint.x < 0 ? 0 : endPoint.x;
    endPoint.y = RAY_LENGTH * cos(float(THREAD_ID) * PI / NUM_THREADS * 2) + lightPos.y;
    endPoint.y = endPoint.y > txrsiz ? txrsiz : endPoint.y;
    endPoint.y = endPoint.y < 0 ? 0 : endPoint.y;
    
    // Bresenham's line algorithm 5-6 FPS @400 lights
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
    
    // vector approximation. 10 FPS @400 lights
    vec2 dt = normalize(endPoint - lightPos);
    vec2 t = vec2(lightPos);
    for (int k = 0; k < RAY_LENGTH; k++) {
        coords.x = int(t.x);
        coords.y = int(t.y);
        
        // calculate transparency
        transpPixel = imageLoad(transpTex, coords);   
        currentAlpha = (transpPixel.b + transpPixel.g * 10.0f + transpPixel.r * 100.0f) / 111.0f;
        // calculate color
        colorPixel = imageLoad(colorTex, coords);
        lightRay.rgb = min(colorPixel.rgb, lightRay.rgb) - (1.0f - currentAlpha) - TRANSMIT; 
        currentOutPixel = imageLoad(img_output, coords);
        currentOutPixel.rgb = max(currentOutPixel.rgb, lightRay.rgb);
        currentOutPixel.a = lightRay.a;
        // write color
        imageStore(img_output, coords, currentOutPixel);
        
        if (currentOutPixel.r + currentOutPixel.g + currentOutPixel.b < 0.05f) break;
        if (dot(vec2(endPoint) - t, dt) < 0.6f) break;
        t += dt;
    }
}

