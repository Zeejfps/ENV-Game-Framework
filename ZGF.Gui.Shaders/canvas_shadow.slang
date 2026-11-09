// Drop-shadow shader for rounded rectangles. Uses Evan Wallace's erf-integral approximation
// (madebyevan.com/shaders/fast-rounded-rectangle-shadows). The shadow rect is the source rect AFTER
// offset/spread have been applied on the CPU; this shader handles only blur + clipping.

#include "canvas_common.slangh"

struct VertexInput
{
    [[vk::location(0)]] float2 unitPos : UNIT_POS;           // [0,1] x [0,1]
    [[vk::location(1)]] float4 outerRect : OUTER_RECT;       // x, y, w, h pixel space (drawn quad)
    [[vk::location(2)]] float4 shadowRect : SHADOW_RECT;     // x, y, w, h pixel space (post offset/spread source rect)
    [[vk::location(3)]] float4 borderRadius : BORDER_RADIUS; // tl, tr, br, bl
    [[vk::location(4)]] float sigma : SIGMA;
    [[vk::location(5)]] uint color : COLOR;                  // ARGB packed
    [[vk::location(6)]] uint clipIndex : CLIP_INDEX;
};

struct Varyings
{
    float4 position : SV_Position;
    float2 pixelPos : PIXEL_POS;
    float4 shadowRect : SHADOW_RECT;
    float4 borderRadius : BORDER_RADIUS;
    nointerpolation float sigma : SIGMA;
    nointerpolation uint color : COLOR;
    nointerpolation uint clipIndex : CLIP_INDEX;
};

[shader("vertex")]
Varyings vertexMain(VertexInput input)
{
    float2 pixelPos = input.outerRect.xy + input.unitPos * input.outerRect.zw;

    Varyings o;
    o.position = project(pixelPos);
    o.pixelPos = pixelPos;
    o.shadowRect = input.shadowRect;
    o.borderRadius = input.borderRadius;
    o.sigma = input.sigma;
    o.color = input.color;
    o.clipIndex = input.clipIndex;
    return o;
}

float2 erf2(float2 x)
{
    float2 s = sign(x);
    float2 a = abs(x);
    float2 v = 1.0 + (0.278393 + (0.230389 + 0.078108 * (a * a)) * a) * a;
    v *= v;
    return s - s / (v * v);
}

float gaussian1D(float x, float sigma)
{
    const float pi = 3.141592653589793;
    return exp(-(x * x) / (2.0 * sigma * sigma)) / (sqrt(2.0 * pi) * sigma);
}

float roundedBoxShadowX(float x, float y, float sigma, float corner, float2 halfSize)
{
    float delta = min(halfSize.y - corner - abs(y), 0.0);
    float curved = halfSize.x - corner + sqrt(max(0.0, corner * corner - delta * delta));
    float2 integral = 0.5 + 0.5 * erf2(float2(x - curved, x + curved) * (sqrt(0.5) / sigma));
    return integral.y - integral.x;
}

float roundedBoxShadow(float2 lower, float2 upper, float2 point, float sigma, float corner)
{
    float2 center = (lower + upper) * 0.5;
    float2 halfSize = (upper - lower) * 0.5;
    float2 p = point - center;

    float low  = p.y - halfSize.y;
    float high = p.y + halfSize.y;
    float start = clamp(-3.0 * sigma, low, high);
    float end   = clamp( 3.0 * sigma, low, high);

    float stepSize = (end - start) / 4.0;
    float y = start + stepSize * 0.5;
    float value = 0.0;
    for (int i = 0; i < 4; i++)
    {
        value += roundedBoxShadowX(p.x, p.y - y, sigma, corner, halfSize) * gaussian1D(y, sigma) * stepSize;
        y += stepSize;
    }
    return value;
}

[shader("fragment")]
float4 fragmentMain(Varyings v) : SV_Target
{
    float clipCover = clipCoverage(v.pixelPos, v.clipIndex);
    if (outsideClipRect(v.pixelPos, v.clipIndex)) discard;
    if (clipCover <= 0.0) discard;

    float2 lower = v.shadowRect.xy;
    float2 upper = lower + v.shadowRect.zw;

    float corner = max(max(v.borderRadius.x, v.borderRadius.y),
                       max(v.borderRadius.z, v.borderRadius.w));
    float maxCorner = min(v.shadowRect.z, v.shadowRect.w) * 0.5;
    corner = min(corner, maxCorner);

    float mass = roundedBoxShadow(lower, upper, v.pixelPos, v.sigma, corner);
    float4 rgba = unpackARGB(v.color);
    return float4(rgba.rgb, rgba.a * clamp(mass, 0.0, 1.0) * clipCover);
}
