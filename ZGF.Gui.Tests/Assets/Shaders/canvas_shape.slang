// Generic anti-aliased SDF primitive: filled circle, ring (stroked circle) and
// line/capsule. The vertex stage expands the unit quad into the instance's
// padded outer rect; the fragment stage evaluates a signed distance field and
// converts it to a one-pixel-feathered coverage alpha. The feather is derived
// from fwidth(d), so the AA band stays one device pixel wide regardless of DPI.
//
// shapeType: 0 = filled circle, 1 = ring, 2 = line/capsule.
//   filled circle: shapeData = (cx, cy, radius, _)   halfWidth unused
//   ring:          shapeData = (cx, cy, radius, _)   halfWidth = half stroke width
//   line:          shapeData = (x0, y0, x1, y1)      halfWidth = half thickness
//
// Cross-target: Slang emits glsl_410 and Metal. The OpenGL backend consumes the
// hand-written canvas_shape.{vert,frag}.glsl in this directory; the Metal backend
// consumes the generated canvas_shape.gen.metal.

struct VertexInput
{
    [[vk::location(0)]] float2 unitPos    : POSITION;
    [[vk::location(1)]] float4 outerRect  : TEXCOORD0;   // x, y, w, h in pixel space (drawn quad)
    [[vk::location(2)]] float4 shapeData  : TEXCOORD1;   // line: p0.xy/p1.xy; circle: center.xy/radius
    [[vk::location(3)]] float  halfWidth  : TEXCOORD2;
    [[vk::location(4)]] uint   color      : COLOR0;      // ARGB packed
    [[vk::location(5)]] uint   shapeType  : COLOR1;
    [[vk::location(6)]] uint   clipIndex  : COLOR2;
};

struct Varyings
{
    float4 position : SV_Position;
    float2 pixelPos : TEXCOORD0;
    float4 shapeData : TEXCOORD1;
    nointerpolation float halfWidth : TEXCOORD2;
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
    if (v.shapeType == 2u)
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
