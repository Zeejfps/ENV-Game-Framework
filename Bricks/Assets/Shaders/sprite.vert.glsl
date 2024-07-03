#version 460

layout(location = 0) in vec4 v_Position;
layout(location = 1) in vec4 v_Normals;

void main() {
    gl_Position = v_Position;
}