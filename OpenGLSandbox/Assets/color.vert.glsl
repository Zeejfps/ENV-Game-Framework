#version 430

layout(location = 0) in vec4 in_Color;
layout(location = 1) in vec4 in_Position;

out vec4 fColor;

void main() {
    fColor = in_Color;
    gl_Position = in_Position;
}