#version 430

in vec4 texCoords;

out vec4 f_Color;

void main() {
    f_Color = texCoords;
}