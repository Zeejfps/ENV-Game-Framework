#version 410

in vec2 v_pixelPos;
in vec2 v_uv;
in vec2 v_local;
flat in vec2 v_halfSize;
flat in vec4 v_srcUV;
flat in float v_fadesEdges;
flat in uint v_tint;
flat in uint v_clipIndex;

layout(std140) uniform ClipRects {
    vec4 u_clipRects[256];
};

uniform sampler2D u_texture;
// Logical canvas size followed by the actual, rounded framebuffer size.
uniform vec4 u_canvasMetrics;

out vec4 f_Color;

vec4 unpackARGB(uint c) {
    float a = float((c >> 24) & 0xFFu) / 255.0;
    float r = float((c >> 16) & 0xFFu) / 255.0;
    float g = float((c >>  8) & 0xFFu) / 255.0;
    float b = float( c        & 0xFFu) / 255.0;
    return vec4(r, g, b, a);
}

void main() {
    vec4 clip = u_clipRects[v_clipIndex];
    if (v_pixelPos.x < clip.x || v_pixelPos.x >= clip.z ||
        v_pixelPos.y < clip.y || v_pixelPos.y >= clip.w) {
        discard;
    }

    // An edge between device pixels fades the pixels it partly covers, by how much it covers them.
    float coverage = 1.0;
    if (v_fadesEdges > 0.0) {
        vec2 devicePerLogical = u_canvasMetrics.zw / u_canvasMetrics.xy;
        vec2 inside = (v_halfSize - abs(v_local)) * devicePerLogical;
        coverage = clamp(inside.x + 0.5, 0.0, 1.0) * clamp(inside.y + 0.5, 0.0, 1.0);
        if (coverage <= 0.0) discard;
    }

    vec2 uv = clamp(v_uv, v_srcUV.xy, v_srcUV.xy + v_srcUV.zw);
    vec4 sample0 = texture(u_texture, uv);
    vec4 tint = unpackARGB(v_tint);
    f_Color = sample0 * tint * vec4(1.0, 1.0, 1.0, coverage);
}
