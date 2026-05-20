#version 410

in vec2 v_pixelPos;
in vec2 v_uv;
flat in uint v_tint;
flat in uint v_clipIndex;

layout(std140) uniform ClipRects {
    vec4 u_clipRects[256];
};

uniform sampler2D u_texture;

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

    vec4 sample0 = texture(u_texture, v_uv);
    vec4 tint = unpackARGB(v_tint);
    f_Color = sample0 * tint;
}
