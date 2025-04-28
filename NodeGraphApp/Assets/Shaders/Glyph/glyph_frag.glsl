#version 330

in vec2 TexCoords;

uniform sampler2D tex;

out vec4 f_Color;

float median(float r, float g, float b) {
    // Efficient median calculation: sorts r, g, b and returns the middle value
    return max(min(r, g), min(max(r, g), b));
}

void main() {

    vec3 msdf = texture(tex, TexCoords).rgb;

    float sd = median(msdf.r, msdf.g, msdf.b);

    float alpha = smoothstep(0.45, 0.55, sd);

    vec4 finalColor = vec4(1, 1, 0.6, 1);
    
    finalColor.a *= alpha;

    f_Color = finalColor;
}