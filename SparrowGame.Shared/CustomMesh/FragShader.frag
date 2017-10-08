
uniform lowp vec2 lightPos;   // light position <0..1>
uniform lowp sampler2D uTexture; // texture unit for light map

varying lowp vec4 vColor; // If this is not here it crashes
varying lowp vec2 vTexCoords; // texture end point, direction <0..1>

//const float transmit = 0.99;  // light transmition coeficient <0..1>
const float transmit = 0.002;  // light transmition coeficient <0..1>
const int txrsiz = 512;       // max texture size [pixels]

void main()
{
    int i;
    vec2 t;
    vec2 dt;
    vec4 currentPixel;
    vec4 outRay = vec4(0, 0, 0, 0);
    for (int lightNum = 0; lightNum < 2; lightNum++) {
        vec4 lightRay = vec4(1.0, 1.0, 1.0, 1.0) - vColor * 0.0000001f;
        t = lightPos;
        t.x = t.x + float(lightNum) * 0.12f;
        
        dt = normalize(vTexCoords - t) / float(txrsiz);
        if (dot(vTexCoords - t, dt) > 0.0) {
            for (i = 0; i < txrsiz; i++) {
                currentPixel = texture2D(uTexture, t);
                lightRay.rgb = min(currentPixel.rgb, lightRay.rgb) - transmit;
                //lightRay.rgb *= (currentPixel.a * currentPixel.rgb) + ((1.0f - currentPixel.a) * transmit);
                
                if (dot(vTexCoords - t, dt) <= 0.00f) break;
                if (lightRay.r + lightRay.g + lightRay.b <= 0.001f) break;
                t += dt;
            }
        }
        outRay = max(outRay, lightRay);
    }
    //gl_FragColor = 0.90 * lightStrength + 0.10 * texture2D(uTexture, vTexCoords);// render with ambient light
    gl_FragColor = outRay; // render without ambient light
    //gl_FragColor = outRay; // render without ambient light
}