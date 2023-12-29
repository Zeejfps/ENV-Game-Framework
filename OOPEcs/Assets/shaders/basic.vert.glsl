#version 430

layout(location = 0) in vec4 v_Position;
layout(location = 1) in vec4 v_Normals;
layout(location = 2) in vec4 v_RectInPixels;

uniform mat4 u_ProjectionMatrix;

void main() {
    float rectX = v_RectInPixels.x;
    float rectY = v_RectInPixels.y;
    float rectWidth = v_RectInPixels.z;
    float rectHeight = v_RectInPixels.w;

    float rectHalfWidth = rectWidth * 0.5f;
    float rectHalfHeight = rectHeight * 0.5f;

    vec4 position = v_Position;
    position.x = (position.x * rectHalfWidth) + rectX;
    position.y = (position.y * rectHalfHeight) + rectY;
    gl_Position = u_ProjectionMatrix * position;
}