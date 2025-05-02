#version 330

uniform vec4 u_Color;

out vec4 f_Color;

void main() {
    f_Color = u_Color;
}