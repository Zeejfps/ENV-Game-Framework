#version 430

layout(location = 0) in vec4 v_Position;
layout(location = 1) in vec4 v_Normals;
layout(location = 2) in vec4 v_Color;
layout(location = 3) in vec4 v_BorderRadius;
layout(location = 4) in vec4 v_RectInPixels;

out vec4 uvs;
out vec4 color;
out vec4 borderRadius;
out vec4 rectInPixels;

void main() {
    uvs = v_Normals;
    color = v_Color;
    borderRadius = v_BorderRadius;
    
    float rectWidth = v_RectInPixels.z;
    float rectHeight = v_RectInPixels.w;
    float aspect = rectWidth / rectHeight;
    
    vec4 position = v_Position;
    position.y /= aspect;
    gl_Position = position;
}