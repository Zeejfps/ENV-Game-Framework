#version 460

layout(location = 0) in vec4 v_Position;
layout(location = 1) in vec4 v_Normals;
layout(location = 2) in vec4 v_ScreenRect;

uniform mat4 projection_matrix;

out vec4 f_uvCoords;

void main() {
    float rectX = v_ScreenRect.x;
    float rectY = v_ScreenRect.y;
    float rectWidth = v_ScreenRect.z;
    float rectHeight = v_ScreenRect.w;

    float rectHalfWidth = rectWidth * 0.5f;
    float rectHalfHeight = rectHeight * 0.5f;

    vec4 position = v_Position;
    position.x = (position.x * rectHalfWidth + rectHalfWidth) + rectX;
    position.y = (position.y * rectHalfHeight + rectHalfHeight) + rectY;

    f_uvCoords = v_Normals;
    
    gl_Position = projection_matrix * position;
}
