// Generic anti-aliased SDF primitive: filled circle, ring (stroked circle) and
// line/capsule. The vertex stage expands the unit quad into the instance's
// padded outer rect; the fragment stage evaluates a signed distance field and
// converts it to a one-pixel-feathered coverage alpha. The feather is derived
// from fwidth(d), so the AA band stays one device pixel wide regardless of DPI.
//
// shapeType: 0 = filled circle, 1 = ring, 2 = line/capsule, 3 = quadratic bezier.
//   filled circle: shapeData = (cx, cy, radius, _)            halfWidth unused
//   ring:          shapeData = (cx, cy, radius, _)            halfWidth = half stroke width
//   line:          shapeData = (x0, y0, x1, y1)               halfWidth = half thickness
//   bezier:        shapeData = (p0.xy, control.xy)            halfWidth = half thickness
//                  shapeData2 = (p2.xy, _, _)
//
// Line-only stroke styling rides in spare fields:
//   shapeData2.zw = (dash length, gap length)   flags bit 2 enables dashing
//   color2        = gradient end color          flags bit 3 enables color->color2 gradient
//   flags bits 0-1 = cap style (0 round, 1 butt, 2 square)
//
// Cross-target: Slang emits glsl_410 and Metal. The OpenGL backend consumes the
// hand-written canvas_shape.{vert,frag}.glsl in this directory; the Metal backend
// consumes the generated canvas_shape.gen.metal.

struct VertexInput
{
    [[vk::location(0)]] float2 unitPos    : POSITION;
    [[vk::location(1)]] float4 outerRect  : TEXCOORD0;   // x, y, w, h in pixel space (drawn quad)
    [[vk::location(2)]] float4 shapeData  : TEXCOORD1;   // line: p0.xy/p1.xy; circle: center.xy/radius; bezier: p0.xy/control.xy
    [[vk::location(3)]] float4 shapeData2 : TEXCOORD2;   // bezier: p2.xy in .xy; line: dash/gap in .zw
    [[vk::location(4)]] float  halfWidth  : TEXCOORD3;
    [[vk::location(5)]] uint   color      : COLOR0;      // ARGB packed
    [[vk::location(6)]] uint   shapeType  : COLOR1;
    [[vk::location(7)]] uint   clipIndex  : COLOR2;
    [[vk::location(8)]] uint   color2     : COLOR3;      // line gradient end
    [[vk::location(9)]] uint   flags      : COLOR4;      // bits 0-1 cap, bit 2 dash, bit 3 gradient
};

struct Varyings
{
    float4 position : SV_Position;
    float2 pixelPos : TEXCOORD0;
    float4 shapeData : TEXCOORD1;
    float4 shapeData2 : TEXCOORD2;
    nointerpolation float halfWidth : TEXCOORD3;
    nointerpolation uint color : COLOR0;
    nointerpolation uint shapeType : COLOR1;
    nointerpolation uint clipIndex : COLOR2;
    nointerpolation uint color2 : COLOR3;
    nointerpolation uint flags : COLOR4;
};

cbuffer Globals
{
    float4x4 u_projection;
}

cbuffer ClipRects
{
    float4 u_clipRects[256];
}

[shader("vertex")]
Varyings vertexMain(VertexInput input)
{
    Varyings o;
    float2 pixelPos = input.outerRect.xy + input.unitPos * input.outerRect.zw;
    o.position = mul(u_projection, float4(pixelPos, 0.0, 1.0));
    o.pixelPos = pixelPos;
    o.shapeData = input.shapeData;
    o.shapeData2 = input.shapeData2;
    o.halfWidth = input.halfWidth;
    o.color = input.color;
    o.shapeType = input.shapeType;
    o.clipIndex = input.clipIndex;
    o.color2 = input.color2;
    o.flags = input.flags;
    return o;
}

float4 unpackARGB(uint c)
{
    float a = float((c >> 24) & 0xFFu) / 255.0;
    float r = float((c >> 16) & 0xFFu) / 255.0;
    float g = float((c >>  8) & 0xFFu) / 255.0;
    float b = float( c        & 0xFFu) / 255.0;
    return float4(r, g, b, a);
}

// Signed distance from point p to the segment a-b.
float sdSegment(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = clamp(dot(pa, ba) / max(dot(ba, ba), 1e-6), 0.0, 1.0);
    return length(pa - ba * h);
}

// Signed distance for a stroked line with selectable cap.
//   cap 0 = round (capsule), 1 = butt (flush rectangle), 2 = square (rectangle + half overhang).
float sdLineCapped(float2 p, float2 a, float2 b, float hw, uint cap)
{
    if (cap == 0u)
        return sdSegment(p, a, b) - hw;

    float2 ba = b - a;
    float len = max(length(ba), 1e-6);
    float2 dir = ba / len;
    float2 rel = p - (a + b) * 0.5;
    float along = abs(dot(rel, dir));
    float perp = abs(dot(rel, float2(-dir.y, dir.x)));
    float capExt = (cap == 2u) ? hw : 0.0;
    float2 q = float2(along - (len * 0.5 + capExt), perp - hw);
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
}

// Unsigned distance from point p to the quadratic Bezier A->B->C (B is the
// control point). Solves the depressed cubic for the closest parameter t.
// Returns (distance, t) so callers can shade along the curve.
// Source: Inigo Quilez (https://www.shadertoy.com/view/MlKcDD).
float2 sdBezier(float2 p, float2 A, float2 B, float2 C)
{
    float2 b = A - 2.0 * B + C;
    // Degenerate (control collinear with endpoints): fall back to a segment,
    // otherwise the 1/dot(b,b) term below explodes.
    if (dot(b, b) < 1e-4)
    {
        float2 ac = C - A;
        float td = clamp(dot(p - A, ac) / max(dot(ac, ac), 1e-6), 0.0, 1.0);
        return float2(sdSegment(p, A, C), td);
    }

    float2 a = B - A;
    float2 e = A - p;
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

    if (h >= 0.0)
    {
        h = sqrt(h);
        float2 x = (float2(h, -h) - q) / 2.0;
        float2 uv = sign(x) * pow(abs(x), float2(1.0 / 3.0, 1.0 / 3.0));
        float t = clamp(uv.x + uv.y - kx, 0.0, 1.0);
        float2 dd = e + (2.0 * a + b * t) * t;
        res = dot(dd, dd);
        bestT = t;
    }
    else
    {
        float z = sqrt(-pp);
        float v = acos(q / (pp * z * 2.0)) / 3.0;
        float m = cos(v);
        float n = sin(v) * 1.732050808;
        float t0 = clamp((m + m) * z - kx, 0.0, 1.0);
        float t1 = clamp((-n - m) * z - kx, 0.0, 1.0);
        float2 d0 = e + (2.0 * a + b * t0) * t0;
        float2 d1 = e + (2.0 * a + b * t1) * t1;
        float dd0 = dot(d0, d0);
        float dd1 = dot(d1, d1);
        res = min(dd0, dd1);
        bestT = dd0 < dd1 ? t0 : t1;
    }
    return float2(sqrt(res), bestT);
}

[shader("fragment")]
float4 fragmentMain(Varyings v) : SV_Target
{
    float4 clip = u_clipRects[v.clipIndex];
    if (v.pixelPos.x < clip.x || v.pixelPos.x >= clip.z ||
        v.pixelPos.y < clip.y || v.pixelPos.y >= clip.w)
    {
        discard;
    }

    float d;
    float bezierT = 0.0;
    if (v.shapeType == 3u)
    {
        // quadratic bezier stroke (round caps/joins)
        float2 bz = sdBezier(v.pixelPos, v.shapeData.xy, v.shapeData.zw, v.shapeData2.xy);
        d = bz.x - v.halfWidth;
        bezierT = bz.y;
    }
    else if (v.shapeType == 2u)
    {
        // line with selectable cap
        d = sdLineCapped(v.pixelPos, v.shapeData.xy, v.shapeData.zw, v.halfWidth, v.flags & 3u);
        if ((v.flags & 4u) != 0u)
        {
            // dashing: carve gaps along the arclength measured from p0
            float2 ba = v.shapeData.zw - v.shapeData.xy;
            float len = max(length(ba), 1e-6);
            float s = dot(v.pixelPos - v.shapeData.xy, ba / len);
            float period = v.shapeData2.z + v.shapeData2.w;
            if (period > 0.0 && (s - period * floor(s / period)) > v.shapeData2.z)
                discard;
        }
    }
    else if (v.shapeType == 1u)
    {
        // ring: distance to the circle outline, then stroke half-width
        d = abs(length(v.pixelPos - v.shapeData.xy) - v.shapeData.z) - v.halfWidth;
    }
    else
    {
        // filled circle
        d = length(v.pixelPos - v.shapeData.xy) - v.shapeData.z;
    }

    // Analytic coverage: 1px transition centered on the zero isoline.
    float aa = max(fwidth(d), 1e-6);
    float coverage = clamp(0.5 - d / aa, 0.0, 1.0);
    if (coverage <= 0.0)
    {
        discard;
    }

    float4 rgba = unpackARGB(v.color);
    if ((v.flags & 8u) != 0u)
    {
        // gradient: lerp color -> color2 along the stroke
        float g;
        if (v.shapeType == 3u)
        {
            g = bezierT;
        }
        else
        {
            float2 ba = v.shapeData.zw - v.shapeData.xy;
            g = clamp(dot(v.pixelPos - v.shapeData.xy, ba) / max(dot(ba, ba), 1e-6), 0.0, 1.0);
        }
        rgba = lerp(rgba, unpackARGB(v.color2), g);
    }
    return float4(rgba.rgb, rgba.a * coverage);
}
