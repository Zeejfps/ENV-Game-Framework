#version 430

layout(location = 0) in vec4 v_Position;
layout(location = 1) in vec4 v_Normals;
layout(location = 2) in vec4 v_Color;

out vec4 uvs;
out vec4 color;

void main() {
    uvs = v_Normals;
    color = v_Color;
    gl_Position = v_Position;
}