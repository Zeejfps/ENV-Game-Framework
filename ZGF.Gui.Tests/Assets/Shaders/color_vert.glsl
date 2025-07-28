#version 400

layout(location = 0) in vec3 position;
layout(location = 1) in vec2 uvs;

uniform mat4 model_matrix;
uniform mat4 view_projection_matrix;

void main() {
    gl_Position = view_projection_matrix * model_matrix * vec4(position, 1.0);
}