#version 430

layout(location = 0) in vec4 v_Position;
layout(location = 1) in vec4 v_Normals;
layout(location = 2) in vec4 v_Color;
layout(location = 3) in vec4 v_BorderRadius;

out vec4 uvs;
out vec4 color;
out vec4 borderRadius;

void main() {
    uvs = v_Normals;
    color = v_Color;
    borderRadius = v_BorderRadius;
    gl_Position = v_Position;
}