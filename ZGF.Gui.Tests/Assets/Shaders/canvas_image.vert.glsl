#version 410

// Per-vertex
layout(location = 0) in vec2 a_unitPos;     // [0,1] x [0,1]

// Per-instance
layout(location = 1) in vec4 i_rect;        // x, y, w, h (pixel space)
layout(location = 2) in vec4 i_srcUV;       // u, v, w, h (normalized texture coords)
layout(location = 3) in uint i_tint;        // ARGB packed
layout(location = 4) in uint i_clipIndex;

uniform mat4 u_projection;

out vec2 v_pixelPos;
out vec2 v_uv;
flat out uint v_tint;
flat out uint v_clipIndex;

void main() {
    vec2 pixelPos = i_rect.xy + a_unitPos * i_rect.zw;
    gl_Position = u_projection * vec4(pixelPos, 0.0, 1.0);

    v_pixelPos = pixelPos;
    v_uv = vec2(
        i_srcUV.x + a_unitPos.x * i_srcUV.z,
        i_srcUV.y + a_unitPos.y * i_srcUV.w
    );
    v_tint = i_tint;
    v_clipIndex = i_clipIndex;
}
