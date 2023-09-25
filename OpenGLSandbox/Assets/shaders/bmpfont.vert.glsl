#version 430

layout(location = 0) in vec4 v_Position;
layout(location = 1) in vec2 v_Normals;
layout(location = 2) in vec4 v_PositionRect;
layout(location = 3) in vec4 v_GlyphRect;

uniform mat4 projection_matrix;

out vec4 color;

void main() {

    float rectX = v_PositionRect.x;
    float rectY = v_PositionRect.y;
    float rectWidth = v_PositionRect.z;
    float rectHeight = v_PositionRect.w;
    float rectHalfWidth = rectWidth * 0.5f;
    float rectHalfHeight = rectHeight * 0.5f;

    vec4 position = v_Position;
    position.x = (position.x * rectHalfWidth + rectHalfWidth) + rectX;
    position.y = (position.y * rectHalfHeight + rectHalfHeight) + rectY;
    gl_Position = projection_matrix * position;

    color = vec4(v_Normals, 0, 1);
}