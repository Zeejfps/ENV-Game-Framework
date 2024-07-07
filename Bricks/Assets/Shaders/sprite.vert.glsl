#version 460

layout(location = 0) in vec4 v_Position;
layout(location = 1) in vec4 v_Normals;
layout(location = 2) in vec4 v_AtlasRect;
layout(location = 3) in vec4 v_ScreenRect;
layout(location = 4) in vec4 v_Tint;

uniform mat4 projection_matrix;
uniform sampler2D tex;

out vec2 f_uvCoords;
out vec4 f_Tint;

void main() {
    ivec2 tex_size = textureSize(tex, 0);

    float rectX = v_ScreenRect.x;
    float rectY = v_ScreenRect.y;
    float rectWidth = v_ScreenRect.z;
    float rectHeight = v_ScreenRect.w;

    float rectHalfWidth = rectWidth * 0.5f;
    float rectHalfHeight = rectHeight * 0.5f;

    vec4 position = v_Position;
    position.x = (position.x * rectHalfWidth + rectHalfWidth) + rectX;
    position.y = (position.y * rectHalfHeight + rectHalfHeight) + rectY;

    float uv_x = v_AtlasRect.z / tex_size.x;
    float uv_y = v_AtlasRect.w / tex_size.y;
    
    f_uvCoords = vec2(v_Normals.x * uv_x, v_Normals.y * uv_y + uv_y);
    f_Tint = v_Tint;
    
    gl_Position = projection_matrix * position;
}
