#version 410

// Per-vertex
layout(location = 0) in vec2 a_unitPos;     // [0,1] x [0,1]

// Per-instance
layout(location = 1) in vec4 i_rect;        // x, y, w, h (pixel space)
layout(location = 2) in vec4 i_atlasUV;     // u, v, w, h (normalized atlas coords)
layout(location = 3) in uint i_color;       // ARGB packed
layout(location = 4) in uint i_clipIndex;
layout(location = 5) in float i_rotation;   // radians, rotation about the glyph rect's center

uniform mat4 u_projection;

out vec2 v_pixelPos;
out vec2 v_atlasUV;
flat out uint v_color;
flat out uint v_clipIndex;

void main() {
    vec2 center = i_rect.xy + i_rect.zw * 0.5;
    vec2 local = (a_unitPos - vec2(0.5)) * i_rect.zw;
    float cs = cos(i_rotation);
    float sn = sin(i_rotation);
    vec2 rotated = vec2(cs * local.x - sn * local.y, sn * local.x + cs * local.y);
    vec2 pixelPos = center + rotated;
    gl_Position = u_projection * vec4(pixelPos, 0.0, 1.0);

    v_pixelPos = pixelPos;
    // Atlas is uploaded with Y already flipped, so the texture's V-axis matches the
    // pixel-space Y-up axis. Bottom of dest rect (unit y=0) samples bottom of glyph in atlas.
    v_atlasUV = vec2(
        i_atlasUV.x + a_unitPos.x * i_atlasUV.z,
        i_atlasUV.y + a_unitPos.y * i_atlasUV.w
    );
    v_color = i_color;
    v_clipIndex = i_clipIndex;
}
