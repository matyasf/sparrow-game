#version 430
            
uniform vec2 lightPos; // passed from the app
            
layout (local_size_x = 512, local_size_y = 1) in; // should be a multiple of 32 on Nvidia, 64 on AMD; >256 might not work
layout (rgba8, binding = 0) uniform image2D img_output;
layout (rgba8, binding = 1) uniform readonly image2D colorTex; // determines color
layout (rgba8, binding = 2) uniform readonly image2D transpTex; // determines transparency

void main () {
    uint global_coords = gl_WorkGroupID.x; // postion in global work group; 0 = left, 1 = right, 2 = top, 3 = bottom
    uint local_coords = gl_LocalInvocationID.x; // get position in local work group
    uint txrsiz = 512; 
    // determine coordinates where rendering ends
    uvec2 endPoint = uvec2(0, 0);
    if (global_coords < 2) {// on the left or right
    endPoint.y = local_coords;
        if (global_coords == 1) { // right
            endPoint.x = 512;
        }
    }
    else {// on the top or bottom
        endPoint.x = local_coords;
        if (global_coords == 3) {
            endPoint.y = 512;
        }
    }
    // calculate light to the endpoint
    uint i;
    vec2 t;
    vec2 dt;
    vec4 outPixel;
    vec4 transpPixel;
    vec4 colorPixel;
    ivec2 coords;
    float transmit = 0;// light transmission constant coeficient <0,1>
    float currentAlpha = 1.0;
    dt = normalize(endPoint - lightPos);
    outPixel = vec4(1.0, 1.0, 1.0, 1.0);  
    t = lightPos;
    if (dot(endPoint-t, dt) > 0.0) {
		for (i = 0; i < txrsiz; i++) {
            coords.x = int(t.x);
            coords.y = int(t.y);
			// calculate transparency
			transpPixel = imageLoad(transpTex, coords);   
            currentAlpha = (transpPixel.b + transpPixel.g * 10.0 + transpPixel.r * 100.0) / 111.0;
			
			// calculate color
			colorPixel = imageLoad(colorTex, coords);
            //outPixel.rgb = colorPixel.rgb;
            outPixel.rgb = min(colorPixel.rgb, outPixel.rgb);
            outPixel.rgb = outPixel.rgb - (1.0 - currentAlpha) - transmit; 

			imageStore(img_output, coords, outPixel);

			if (dot(endPoint - t, dt) <= 0.000f) break;
			//if (outPixel.r + outPixel.g + outPixel.b <= 0.001f) break;
			t += dt;
		}
    }
}