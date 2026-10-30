#version 410

// Per-vertex
layout(location = 0) in vec2 a_unitPos;     // [0,1] x [0,1]

// Per-instance
layout(location = 1) in vec4 i_rect;        // x, y, w, h (pixel space)
layout(location = 2) in vec4 i_srcUV;       // u, v, w, h (normalized texture coords)
layout(location = 3) in uint i_tint;        // ARGB packed
layout(location = 4) in uint i_clipIndex;
layout(location = 5) in float i_rotation;   // radians, rotation about the rect's center
layout(location = 6) in vec4 i_cornerRadius; // tl, tr, br, bl

uniform mat4 u_projection;
// Logical canvas size followed by the actual, rounded framebuffer size.
uniform vec4 u_canvasMetrics;

out vec2 v_pixelPos;
out vec2 v_uv;
// Where the fragment is from the image's center in its own unrotated frame, and half its size.
out vec2 v_local;
flat out vec2 v_halfSize;
// The image's own texels, which a grown edge is clamped to; and whether its edges fade at all — only
// when the quad grew, so an image on the grid, or rotated, is drawn exactly as before.
flat out vec4 v_srcUV;
flat out float v_fadesEdges;
flat out uint v_tint;
flat out uint v_clipIndex;
flat out vec4 v_cornerRadius;

void main() {
    // An unrotated image a transform left between device pixels grows a pixel on that axis, so the
    // fragment shader can fade the pixels its edge partly covers; one on the grid stays as it was.
    vec2 devicePerLogical = u_canvasMetrics.zw / u_canvasMetrics.xy;
    vec2 low = i_rect.xy * devicePerLogical;
    vec2 high = (i_rect.xy + i_rect.zw) * devicePerLogical;
    vec2 offGrid = max(abs(low - round(low)), abs(high - round(high)));
    bool growable = i_rotation == 0.0 && i_rect.z > 0.0 && i_rect.w > 0.0;
    vec2 outset = growable ? step(1.0 / 256.0, offGrid) / devicePerLogical : vec2(0.0);
    vec2 unit = (a_unitPos * (i_rect.zw + 2.0 * outset) - outset) / max(i_rect.zw, vec2(1e-6));

    vec2 center = i_rect.xy + i_rect.zw * 0.5;
    vec2 local = (unit - vec2(0.5)) * i_rect.zw;
    float cs = cos(i_rotation);
    float sn = sin(i_rotation);
    vec2 rotated = vec2(cs * local.x - sn * local.y, sn * local.x + cs * local.y);
    vec2 pixelPos = center + rotated;
    gl_Position = u_projection * vec4(pixelPos, 0.0, 1.0);

    v_pixelPos = pixelPos;
    // Past the image's edge the texture clamps, so the grown pixels show its edge texels, faded.
    v_uv = vec2(
        i_srcUV.x + unit.x * i_srcUV.z,
        i_srcUV.y + unit.y * i_srcUV.w
    );
    v_local = local;
    v_halfSize = i_rect.zw * 0.5;
    v_srcUV = i_srcUV;
    v_fadesEdges = outset.x > 0.0 || outset.y > 0.0 ? 1.0 : 0.0;
    v_tint = i_tint;
    v_clipIndex = i_clipIndex;
    v_cornerRadius = min(i_cornerRadius, vec4(min(i_rect.z, i_rect.w) * 0.5));
}
