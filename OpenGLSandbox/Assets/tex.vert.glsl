#version 430

layout(location = 0) in vec4 v_Position;
layout(location = 1) in vec4 v_Normals;

out vec4 texCoords;

void main() {
    texCoords = v_Normals;
    gl_Position = v_Position;
}