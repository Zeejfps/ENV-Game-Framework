#version 330

in vec2 TexCoords;

uniform sampler2D tex;

out vec4 f_Color;

void main() {
    float a = texture(tex, TexCoords).b;
    f_Color = vec4(a, 0.0, 0.0, 1.0);
}