#version 410

// Generic anti-aliased SDF primitive: filled circle, ring (stroked circle) and
// line/capsule. See canvas_shape.slang for the authoritative source; the OpenGL
// backend uses this hand-written GLSL, matching the rest of the canvas pipeline.

in vec2 v_pixelPos;
in vec4 v_shapeData;
in vec4 v_shapeData2;
flat in float v_halfWidth;
flat in uint v_color;
flat in uint v_shapeType;
flat in uint v_clipIndex;
flat in uint v_color2;
flat in uint v_flags;

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

// Stroked line with selectable cap: 0 round (capsule), 1 butt, 2 square.
float sdLineCapped(vec2 p, vec2 a, vec2 b, float hw, uint cap) {
    if (cap == 0u) return sdSegment(p, a, b) - hw;
    vec2 ba = b - a;
    float len = max(length(ba), 1e-6);
    vec2 dir = ba / len;
    vec2 rel = p - (a + b) * 0.5;
    float along = abs(dot(rel, dir));
    float perp = abs(dot(rel, vec2(-dir.y, dir.x)));
    float capExt = (cap == 2u) ? hw : 0.0;
    vec2 q = vec2(along - (len * 0.5 + capExt), perp - hw);
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
}

// Distance + closest parameter t for the quadratic Bezier A->B->C (B = control).
// Returns (distance, t). Source: Inigo Quilez (https://www.shadertoy.com/view/MlKcDD).
vec2 sdBezier(vec2 p, vec2 A, vec2 B, vec2 C) {
    vec2 b = A - 2.0 * B + C;
    if (dot(b, b) < 1e-4) { // control collinear -> segment
        vec2 ac = C - A;
        float td = clamp(dot(p - A, ac) / max(dot(ac, ac), 1e-6), 0.0, 1.0);
        return vec2(sdSegment(p, A, C), td);
    }
    vec2 a = B - A;
    vec2 e = A - p;
    float kk = 1.0 / dot(b, b);
    float kx = kk * dot(a, b);
    float ky = kk * (2.0 * dot(a, a) + dot(e, b)) / 3.0;
    float kz = kk * dot(e, a);
    float res;
    float bestT;
    float pp = ky - kx * kx;
    float pp3 = pp * pp * pp;
    float q = kx * (2.0 * kx * kx - 3.0 * ky) + kz;
    float h = q * q + 4.0 * pp3;
    if (h >= 0.0) {
        h = sqrt(h);
        vec2 x = (vec2(h, -h) - q) / 2.0;
        vec2 uv = sign(x) * pow(abs(x), vec2(1.0 / 3.0));
        float t = clamp(uv.x + uv.y - kx, 0.0, 1.0);
        vec2 dd = e + (2.0 * a + b * t) * t;
        res = dot(dd, dd);
        bestT = t;
    } else {
        float z = sqrt(-pp);
        float v = acos(q / (pp * z * 2.0)) / 3.0;
        float m = cos(v);
        float n = sin(v) * 1.732050808;
        float t0 = clamp((m + m) * z - kx, 0.0, 1.0);
        float t1 = clamp((-n - m) * z - kx, 0.0, 1.0);
        vec2 d0 = e + (2.0 * a + b * t0) * t0;
        vec2 d1 = e + (2.0 * a + b * t1) * t1;
        float dd0 = dot(d0, d0);
        float dd1 = dot(d1, d1);
        res = min(dd0, dd1);
        bestT = dd0 < dd1 ? t0 : t1;
    }
    return vec2(sqrt(res), bestT);
}

void main() {
    vec4 clip = u_clipRects[v_clipIndex];
    if (v_pixelPos.x < clip.x || v_pixelPos.x >= clip.z ||
        v_pixelPos.y < clip.y || v_pixelPos.y >= clip.w) {
        discard;
    }

    float d;
    float bezierT = 0.0;
    if (v_shapeType == 3u) {
        // quadratic bezier stroke (round caps/joins)
        vec2 bz = sdBezier(v_pixelPos, v_shapeData.xy, v_shapeData.zw, v_shapeData2.xy);
        d = bz.x - v_halfWidth;
        bezierT = bz.y;
    } else if (v_shapeType == 2u) {
        // line with selectable cap
        d = sdLineCapped(v_pixelPos, v_shapeData.xy, v_shapeData.zw, v_halfWidth, v_flags & 3u);
        if ((v_flags & 4u) != 0u) {
            // dashing: carve gaps along the arclength measured from p0
            vec2 ba = v_shapeData.zw - v_shapeData.xy;
            float len = max(length(ba), 1e-6);
            float s = dot(v_pixelPos - v_shapeData.xy, ba / len);
            float period = v_shapeData2.z + v_shapeData2.w;
            if (period > 0.0 && (s - period * floor(s / period)) > v_shapeData2.z) discard;
        }
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
    if ((v_flags & 8u) != 0u) {
        // gradient: mix color -> color2 along the stroke
        float g;
        if (v_shapeType == 3u) {
            g = bezierT;
        } else {
            vec2 ba = v_shapeData.zw - v_shapeData.xy;
            g = clamp(dot(v_pixelPos - v_shapeData.xy, ba) / max(dot(ba, ba), 1e-6), 0.0, 1.0);
        }
        rgba = mix(rgba, unpackARGB(v_color2), g);
    }
    f_Color = vec4(rgba.rgb, rgba.a * coverage);
}
