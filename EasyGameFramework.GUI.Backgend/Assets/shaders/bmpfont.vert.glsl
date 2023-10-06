#version 430

// Per Vertex attributes
layout(location = 0) in vec4 v_Position;
layout(location = 1) in vec2 v_Normals;

// Instanced attributes
layout(location = 2) in vec4 v_PositionRect;
layout(location = 3) in vec4 v_GlyphRect;
layout(location = 4) in vec4 v_Color;

// Uniforms
layout(location = 0) uniform mat4 u_ProjectionMatrix;
layout(location = 1) uniform vec4 u_GlyphSheetSize;

out vec2 TexCoords;
out vec4 Color;

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
    gl_Position = u_ProjectionMatrix * position;

    TexCoords = v_Normals;
    TexCoords.x = TexCoords.x * v_GlyphRect.z + v_GlyphRect.x;
    TexCoords.y = (1.0 - TexCoords.y) * v_GlyphRect.w + v_GlyphRect.y;
    
    Color = v_Color;
}