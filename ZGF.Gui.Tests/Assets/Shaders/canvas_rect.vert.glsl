#version 460

// Per-vertex
layout(location = 0) in vec2 a_unitPos;          // [0,1] x [0,1]

// Per-instance
layout(location = 1) in vec4 i_rect;             // x, y, w, h (pixel space)
layout(location = 2) in vec4 i_borderRadius;     // tl, tr, br, bl
layout(location = 3) in vec4 i_borderSize;       // top, right, bottom, left
layout(location = 4) in uint i_bgColor;          // ARGB packed
layout(location = 5) in uint i_borderColorTop;
layout(location = 6) in uint i_borderColorRight;
layout(location = 7) in uint i_borderColorBottom;
layout(location = 8) in uint i_borderColorLeft;
layout(location = 9) in uint i_clipIndex;

uniform mat4 u_projection;

out vec2 v_pixelPos;       // world (pixel) position for clip test
out vec2 v_localPos;       // 0..rectW, 0..rectH
out vec4 v_rectSize;       // (w, h, w, h) for convenience
out vec4 v_borderRadius;
out vec4 v_borderSize;
flat out uint v_bgColor;
flat out uint v_borderColorTop;
flat out uint v_borderColorRight;
flat out uint v_borderColorBottom;
flat out uint v_borderColorLeft;
flat out uint v_clipIndex;

void main() {
    vec2 pixelPos = i_rect.xy + a_unitPos * i_rect.zw;
    gl_Position = u_projection * vec4(pixelPos, 0.0, 1.0);

    v_pixelPos = pixelPos;
    v_localPos = a_unitPos * i_rect.zw;
    v_rectSize = vec4(i_rect.zw, i_rect.zw);
    v_borderRadius = i_borderRadius;
    v_borderSize = i_borderSize;
    v_bgColor = i_bgColor;
    v_borderColorTop = i_borderColorTop;
    v_borderColorRight = i_borderColorRight;
    v_borderColorBottom = i_borderColorBottom;
    v_borderColorLeft = i_borderColorLeft;
    v_clipIndex = i_clipIndex;
}
