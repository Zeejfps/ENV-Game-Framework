#version 460

layout(location = 0) in vec4 v_Position;
layout(location = 1) in vec4 v_Normals;

uniform mat4 projection_matrix;

void main() {
    gl_Position = projection_matrix * v_Position;
}