#version 410

// Per-vertex
layout(location = 0) in vec2 a_unitPos;          // [0,1] x [0,1]

// Per-instance
layout(location = 1) in vec4 i_outerRect;        // x, y, w, h pixel space (drawn quad)
layout(location = 2) in vec4 i_shadowRect;       // x, y, w, h pixel space (post offset/spread source rect)
layout(location = 3) in vec4 i_borderRadius;     // tl, tr, br, bl
layout(location = 4) in float i_sigma;
layout(location = 5) in uint i_color;            // ARGB packed
layout(location = 6) in uint i_clipIndex;

uniform mat4 u_projection;

out vec2 v_pixelPos;
out vec4 v_shadowRect;
out vec4 v_borderRadius;
flat out float v_sigma;
flat out uint v_color;
flat out uint v_clipIndex;

void main() {
    vec2 pixelPos = i_outerRect.xy + a_unitPos * i_outerRect.zw;
    gl_Position = u_projection * vec4(pixelPos, 0.0, 1.0);

    v_pixelPos = pixelPos;
    v_shadowRect = i_shadowRect;
    v_borderRadius = i_borderRadius;
    v_sigma = i_sigma;
    v_color = i_color;
    v_clipIndex = i_clipIndex;
}
