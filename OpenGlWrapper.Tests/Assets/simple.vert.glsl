#version 460 core

layout (location = 0) in vec3 v_Position;
layout (location = 1) in vec2 v_UVs;

out vec2 UVs;

void main() {
    gl_Position = vec4(v_Position, 1.0);
    UVs = v_UVs;
}