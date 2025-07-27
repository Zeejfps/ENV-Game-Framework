#version 400

layout(location = 0) in vec4 position;
layout(location = 1) in vec2 uvs;

uniform mat4 view_projection_matrix;

void main() {
    gl_Position = view_projection_matrix * position;
}