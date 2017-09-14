
uniform lowp vec2 lightPos;   // light position <0..1>
uniform lowp sampler2D uTexture; // texture unit for light map

varying lowp vec4 vColor;
varying lowp vec2 vTexCoords; // texture end point, direction <0..1>

const float transmit = 0.99;  // light transmition coeficient <0..1>
const int txrsiz = 512;       // max texture size [pixels]

void main()
{
    int i;
    vec2 t,dt;
    vec4 currentPixel;
    vec4 lightStrength = vec4(1.0, 1.0, 1.0, 1.0) - vColor * 0.0000001f;
    dt = normalize(vTexCoords - lightPos) / float(txrsiz);
    t = lightPos;
    if (dot(vTexCoords - t, dt) > 0.0) {
        for (i = 0; i < txrsiz; i++) {
            currentPixel = texture2D(uTexture, t);
            //lightStrength.rgb = lightStrength.rgb - (1.0f - currentPixel.rgb);
            lightStrength.rgb *= (currentPixel.a * currentPixel.rgb) + ((1.0f - currentPixel.a) * transmit);
            
            if (dot(vTexCoords - t, dt) <= 0.00f) break;
            if (lightStrength.r + lightStrength.g + lightStrength.b <= 0.001f) break;
            t += dt;
        }
    }
    //gl_FragColor = 0.90 * lightStrength + 0.10 * texture2D(uTexture, vTexCoords);// render with ambient light
    gl_FragColor = lightStrength; // render without ambient light
}