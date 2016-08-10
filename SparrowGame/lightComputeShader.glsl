#version 430
            
uniform vec2 lightPos; // passed from the app
            
layout (local_size_x = 512, local_size_y = 1) in; // should be a multiple of 32 on Nvidia, 64 on AMD; >256 might not work
layout (rgba8, binding = 0) uniform image2D img_output;
layout (rgba8, binding = 1) uniform readonly image2D bgTex; // determines color
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
    vec2 t,dt;
    vec4 c0,c1;
    float transmit = 0.1;// light transmition coeficient <0,1>
    dt = normalize(endPoint - lightPos); // / float(txrsiz);
    c0 = vec4(1.0, 1.0, 1.0, 1.0);   // light ray strength
    t = lightPos;
    if (dot(endPoint-t, dt) > 0.0) {
		for (i = 0; i < txrsiz; i++) {
            ivec2 coords = ivec2(t.x, t.y);
			c1 = imageLoad(transpTex, coords); // TODO use this to calc alpha only. Use bgTex to calc color.
			//c0.rgb *= ((c1.a)*(c1.rgb)) + ((1.0f-c1.a)*transmit);
			c0.rgb *= c1.rgb;
			imageStore(img_output, coords, c0);

			if (dot(endPoint-t, dt) <= 0.000f) break;
			if (c0.r+c0.g+c0.b <= 0.001f) break;
			t += dt;
		}
    }
/*
    col = 0.90*c0+0.10*texture2D(txrmap, endPoint);  // render with ambient light
    //  col=c0;  // render without ambient light

    vec4 bgpix = imageLoad(bgTex, global_coords);
    vec2 lightP = lightPos / 100;
    vec4 pixel = vec4(bgpix.r, lightP.x, lightP.y, 0.8);

    // output to a specific pixel in the image
    imageStore(img_output, global_coords, pixel);*/
}