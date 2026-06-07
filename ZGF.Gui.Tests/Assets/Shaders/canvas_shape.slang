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
// Cross-target: Slang emits glsl_410 and Metal. The OpenGL backend consumes the
// hand-written canvas_shape.{vert,frag}.glsl in this directory; the Metal backend
// consumes the generated canvas_shape.gen.metal.

struct VertexInput
{
    [[vk::location(0)]] float2 unitPos    : POSITION;
    [[vk::location(1)]] float4 outerRect  : TEXCOORD0;   // x, y, w, h in pixel space (drawn quad)
    [[vk::location(2)]] float4 shapeData  : TEXCOORD1;   // line: p0.xy/p1.xy; circle: center.xy/radius; bezier: p0.xy/control.xy
    [[vk::location(3)]] float4 shapeData2 : TEXCOORD2;   // bezier: p2.xy in .xy
    [[vk::location(4)]] float  halfWidth  : TEXCOORD3;
    [[vk::location(5)]] uint   color      : COLOR0;      // ARGB packed
    [[vk::location(6)]] uint   shapeType  : COLOR1;
    [[vk::location(7)]] uint   clipIndex  : COLOR2;
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

// Unsigned distance from point p to the quadratic Bezier A->B->C (B is the
// control point). Solves the depressed cubic for the closest parameter t.
// Source: Inigo Quilez (https://www.shadertoy.com/view/MlKcDD).
float sdBezier(float2 p, float2 A, float2 B, float2 C)
{
    float2 b = A - 2.0 * B + C;
    // Degenerate (control collinear with endpoints): fall back to a segment,
    // otherwise the 1/dot(b,b) term below explodes.
    if (dot(b, b) < 1e-4)
        return sdSegment(p, A, C);

    float2 a = B - A;
    float2 e = A - p;
    float kk = 1.0 / dot(b, b);
    float kx = kk * dot(a, b);
    float ky = kk * (2.0 * dot(a, a) + dot(e, b)) / 3.0;
    float kz = kk * dot(e, a);

    float res;
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
        res = min(dot(d0, d0), dot(d1, d1));
    }
    return sqrt(res);
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
    if (v.shapeType == 3u)
    {
        // quadratic bezier stroke (round caps/joins)
        d = sdBezier(v.pixelPos, v.shapeData.xy, v.shapeData.zw, v.shapeData2.xy) - v.halfWidth;
    }
    else if (v.shapeType == 2u)
    {
        // line / capsule (round caps)
        d = sdSegment(v.pixelPos, v.shapeData.xy, v.shapeData.zw) - v.halfWidth;
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
    return float4(rgba.rgb, rgba.a * coverage);
}
