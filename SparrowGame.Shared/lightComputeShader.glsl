#version 310 es
//#version 450
// what if a light source comes in from outside of the map? -- it needs much bigger tex size (~1500x1500) and just a small part is visible
            
layout (local_size_x = 128, local_size_y = 1) in; // product of all must be max 128. Local size of the shader
layout (rgba8, binding = 0) uniform writeonly highp image2D img_output;
layout (rgba8, binding = 1) uniform readonly highp image2D colorTex; // determines color
layout (rgba8, binding = 2) uniform readonly highp image2D transpTex; // determines transparency

#define MAX_NUM_TOTAL_LIGHTS 10
struct LightData {
    vec4 lightColor; // light color and alpha
    vec2 lightPos; // light pos
};

layout (std140, binding = 3) uniform Lights {
    LightData light[MAX_NUM_TOTAL_LIGHTS];
};

uniform uint lightNum;

void main () {
    float global_coords = float(gl_WorkGroupID.x); // postion in global work group; 0 = left, 1 = right, 2 = top, 3 = bottom
	float global_coords2 = float(gl_WorkGroupID.y); // position in second global wg, determines which segment to render (0..3)
    float local_coords = float(gl_LocalInvocationID.x); // get position in local work group
	vec2 bgSize =  vec2(imageSize(colorTex));
    float txrsiz = 512.0f; 
    // determine coordinates where rendering ends
    vec2 endPoint = vec2(0.0f, 0.0f);
    if (global_coords < 2.0f) {// on the left or right
	    endPoint.y = local_coords + global_coords2 * 128.0f;
        if (global_coords == 1.0f) { // right
            endPoint.x = txrsiz;
        }
    }
    else {// on the top or bottom
        endPoint.x = local_coords + global_coords2 * 128.0f;
        if (global_coords == 3.0f) {
            endPoint.y = txrsiz;
        }
    }

    // calculate light to the endpoint
    vec2 t;
    vec2 dt;
    vec4 lightRay; // initial light color
    vec4 transpPixel;
    vec4 colorPixel;
	vec4 currentPixel;
    ivec2 coords; 
    float transmit = 0.0f;// light transmission constant coeficient <0,1>
    float currentAlpha;
	for (uint i = 0u; i < lightNum; i++) {
	    lightRay = light[i].lightColor; 
        dt = normalize(endPoint - light[i].lightPos);
        t = light[i].lightPos;
        
    	for (float i = 0.0f; i < txrsiz; i++) {
    		if (dot(endPoint - t, dt) < 0.5f) break;
    
    		coords.x = int(t.x);
    		coords.y = int(t.y);
    
    		// calculate transparency
    		transpPixel = imageLoad(transpTex, coords);   
    		currentAlpha = (transpPixel.b + transpPixel.g * 10.0 + transpPixel.r * 100.0) / 111.0;
    			
    		// calculate color
    		colorPixel = imageLoad(colorTex, coords);
    		lightRay.rgb = min(colorPixel.rgb, lightRay.rgb) - (1.0 - currentAlpha) - transmit; 
    			
    		// write color
    		imageStore(img_output, coords, lightRay);
    
    		//if (dot(endPoint - t, dt) <= 0.0f) break;
    		//if (lightRay.r + lightRay.g + lightRay.b <= 0.001f) break;
    		t += dt;
    	}
	}
	
    
}