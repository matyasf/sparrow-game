#version 310 es
            
uniform highp vec2 lightPos; // passed from the app
            
layout (local_size_x = 512, local_size_y = 1) in; // should be a multiple of 32 on Nvidia, 64 on AMD; >256 might not work
layout (rgba8, binding = 0) uniform restrict writeonly highp image2D img_output;
layout (rgba8, binding = 1) uniform readonly highp image2D colorTex; // determines color
layout (rgba8, binding = 2) uniform readonly highp image2D transpTex; // determines transparency

void main () {
    float global_coords = float(gl_WorkGroupID.x); // postion in global work group; 0 = left, 1 = right, 2 = top, 3 = bottom
    float local_coords = float(gl_LocalInvocationID.x); // get position in local work group
    float txrsiz = 512.0f; 
    // determine coordinates where rendering ends
    vec2 endPoint = vec2(0.0f, 0.0f);
    if (global_coords < 2.0f) {// on the left or right
    endPoint.y = local_coords;
        if (global_coords == 1.0f) { // right
            endPoint.x = txrsiz;
        }
    }
    else {// on the top or bottom
        endPoint.x = local_coords;
        if (global_coords == 3.0f) {
            endPoint.y = txrsiz;
        }
    }
    // calculate light to the endpoint
    float i;
    vec2 t;
    vec2 dt;
    vec4 outPixel; // TODO must be uvec or ivec on Android?
    vec4 transpPixel;
    vec4 colorPixel;
    ivec2 coords;
    float transmit = 0.0f;// light transmission constant coeficient <0,1>
    float currentAlpha = 1.0f;
    dt = normalize(endPoint - lightPos);
    outPixel = vec4(1.0, 1.0, 1.0, 1.0);  
    t = lightPos;
    if (dot(endPoint - t, dt) > 0.0f) {
		for (i = 0.0f; i < txrsiz; i++) {
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
			
			//uvec4 iOut = uvec4(uint(outPixel.r * 256.0), uint(outPixel.g * 256.0), uint(outPixel.b * 256.0), uint(outPixel.a * 256.0));
			imageStore(img_output, coords, outPixel);

			if (dot(endPoint - t, dt) <= 0.000f) break;
			//if (outPixel.r + outPixel.g + outPixel.b <= 0.001f) break;
			t += dt;
		}
    }
}