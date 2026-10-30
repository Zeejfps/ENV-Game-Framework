#version 410

in vec2 v_pixelPos;
in vec2 v_atlasUV;
flat in uint v_color;
flat in uint v_clipIndex;

layout(std140) uniform ClipRects {
    vec4 u_clipRects[256];  // (left, bottom, right, top)
    vec4 u_clipRounds[256]; // the rounded rect the clip is also cut to, (left, bottom, right, top)
    vec4 u_clipRadii[256];  // that rect's corner radius in x; 0 when the clip is not rounded
};

uniform sampler2D u_atlas;

out vec4 f_Color;

vec4 unpackARGB(uint c) {
    float a = float((c >> 24) & 0xFFu) / 255.0;
    float r = float((c >> 16) & 0xFFu) / 255.0;
    float g = float((c >>  8) & 0xFFu) / 255.0;
    float b = float( c        & 0xFFu) / 255.0;
    return vec4(r, g, b, a);
}

// How much of the pixel at p the clip's rounded rect covers: 1 inside it, 0 outside, and between over
// the device pixel its edge crosses. Always 1 for a clip that is not rounded.
float clipCoverage(vec2 p, uint index) {
    float pixel = max(fwidth(p.x), 1e-4);
    float r = u_clipRadii[index].x;
    if (r <= 0.0) return 1.0;
    vec4 rounded = u_clipRounds[index];
    vec2 halfSize = (rounded.zw - rounded.xy) * 0.5;
    vec2 q = abs(p - (rounded.xy + rounded.zw) * 0.5) - (halfSize - vec2(r));
    float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
    return clamp(0.5 - d / pixel, 0.0, 1.0);
}

void main() {
    float clipCover = clipCoverage(v_pixelPos, v_clipIndex);
    vec4 clip = u_clipRects[v_clipIndex];
    if (v_pixelPos.x < clip.x || v_pixelPos.x >= clip.z ||
        v_pixelPos.y < clip.y || v_pixelPos.y >= clip.w) {
        discard;
    }
    if (clipCover <= 0.0) discard;

    float alpha = texture(u_atlas, v_atlasUV).r;
    if (alpha <= 0.0) discard;

    vec4 color = unpackARGB(v_color);
    f_Color = vec4(color.rgb, color.a * alpha);
    f_Color.a *= clipCover;
}
