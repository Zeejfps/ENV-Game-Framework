#version 430

layout(location = 0) in vec4 v_Color;
layout(location = 1) in vec4 v_Position;

out vec4 color;

void main() {
    color = v_Color;
    gl_Position = v_Position;
}