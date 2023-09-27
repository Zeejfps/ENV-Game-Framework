#version 430

in vec2 TexCoords;
in vec4 Color;

uniform sampler2D tex;

out vec4 f_Color;

void main() {
    float a = texture(tex, TexCoords).r;
    f_Color = vec4(Color.rgb, a);
}