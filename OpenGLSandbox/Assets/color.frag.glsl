#version 430

in vec4 fColor;

out vec4 out_Color;

void main() {
    out_Color = vec4(fColor.r, fColor.g, 0.5, 1.0);
}