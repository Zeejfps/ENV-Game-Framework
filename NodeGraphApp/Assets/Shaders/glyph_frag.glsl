#version 330

in vec2 TexCoords;

uniform sampler2D tex;

out vec4 f_Color;

float median(float r, float g, float b) {
    return max(min(r, g), min(max(r, g), b));
}

float screenPxRange() {
    vec2 unitRange = vec2(6.0) / vec2(textureSize(tex, 0));
    vec2 screenTexSize = vec2(1.0) / fwidth(TexCoords);
    return max(0.5 * dot(unitRange, screenTexSize), 1.0);
}

void main() {

    vec3 msdf = texture(tex, TexCoords).rgb;

    float sd = median(msdf.r, msdf.g, msdf.b);

    float pxDist = screenPxRange() * (sd - 0.5);
  
    float opacity = clamp(pxDist+0.5, 0.0, 1.0);

    f_Color = vec4(0.6, 0.8, 0.7, opacity);
}