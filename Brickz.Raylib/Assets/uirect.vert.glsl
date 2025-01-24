#version 460

layout(location = 0) in vec4 vertexPosition;
layout(location = 1) in vec4 vertexNormal;

uniform vec4 v_Color;
uniform vec4 v_BorderRadius;
uniform vec4 v_RectInPixels;
uniform vec4 v_BorderColor;
uniform vec4 v_BorderSize;

uniform mat4 matProjection;

out vec4 uvs;
out vec4 color;
out vec4 borderColor;
out vec4 borderRadius;
out vec4 borderSize;
out vec4 rectInPixels;

void main() {
    uvs = vertexNormal;
    color = v_Color;
    borderSize = v_BorderSize;
    borderColor = v_BorderColor;
    borderRadius = v_BorderRadius;
    rectInPixels = v_RectInPixels;
    
    float rectX = v_RectInPixels.x;
    float rectY = v_RectInPixels.y;
    float rectWidth = v_RectInPixels.z;
    float rectHeight = v_RectInPixels.w;
    
    float rectHalfWidth = rectWidth * 0.5f;
    float rectHalfHeight = rectHeight * 0.5f;
    
    vec4 position = vertexPosition;
    position.x = (position.x * rectHalfWidth + rectHalfWidth) + rectX;
    position.y = (position.y * rectHalfHeight + rectHalfHeight) + rectY;
    gl_Position = matProjection * position;
}