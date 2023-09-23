#version 430

layout(location = 0) in vec4 v_Position;
layout(location = 1) in vec2 v_Normals;

out vec2 texCoords;

void main() {
    texCoords = vec2(v_Normals.x, -v_Normals.y);
    gl_Position = v_Position;
}