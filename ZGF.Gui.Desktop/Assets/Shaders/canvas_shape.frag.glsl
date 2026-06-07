#version 410

// Generic anti-aliased SDF primitive: filled circle, ring (stroked circle) and
// line/capsule. See canvas_shape.slang for the authoritative source; the OpenGL
// backend uses this hand-written GLSL, matching the rest of the canvas pipeline.

in vec2 v_pixelPos;
in vec4 v_shapeData;
flat in float v_halfWidth;
flat in uint v_color;
flat in uint v_shapeType;
flat in uint v_clipIndex;

layout(std140) uniform ClipRects {
    vec4 u_clipRects[256]; // (left, bottom, right, top)
};

out vec4 f_Color;

vec4 unpackARGB(uint c) {
    float a = float((c >> 24) & 0xFFu) / 255.0;
    float r = float((c >> 16) & 0xFFu) / 255.0;
    float g = float((c >>  8) & 0xFFu) / 255.0;
    float b = float( c        & 0xFFu) / 255.0;
    return vec4(r, g, b, a);
}

float sdSegment(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / max(dot(ba, ba), 1e-6), 0.0, 1.0);
    return length(pa - ba * h);
}

void main() {
    vec4 clip = u_clipRects[v_clipIndex];
    if (v_pixelPos.x < clip.x || v_pixelPos.x >= clip.z ||
        v_pixelPos.y < clip.y || v_pixelPos.y >= clip.w) {
        discard;
    }

    float d;
    if (v_shapeType == 2u) {
        // line / capsule (round caps)
        d = sdSegment(v_pixelPos, v_shapeData.xy, v_shapeData.zw) - v_halfWidth;
    } else if (v_shapeType == 1u) {
        // ring: distance to the circle outline, then stroke half-width
        d = abs(length(v_pixelPos - v_shapeData.xy) - v_shapeData.z) - v_halfWidth;
    } else {
        // filled circle
        d = length(v_pixelPos - v_shapeData.xy) - v_shapeData.z;
    }

    // Analytic coverage: 1px transition centered on the zero isoline.
    float aa = max(fwidth(d), 1e-6);
    float coverage = clamp(0.5 - d / aa, 0.0, 1.0);
    if (coverage <= 0.0) discard;

    vec4 rgba = unpackARGB(v_color);
    f_Color = vec4(rgba.rgb, rgba.a * coverage);
}
