attribute vec4 aPosition;
attribute vec4 aColor;
attribute vec2 aTexCoords;

uniform mat4 uMvpMatrix;
uniform vec4 uAlpha;
 
varying lowp vec4 vColor;
varying lowp vec2 vTexCoords;

void main()
{
    gl_Position = uMvpMatrix * aPosition;
    vColor = aColor * uAlpha;
    vTexCoords = aTexCoords;
}