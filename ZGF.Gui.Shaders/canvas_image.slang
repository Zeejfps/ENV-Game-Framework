#include "canvas_common.slangh"

[[vk::binding(2)]] Sampler2D u_texture : register(t0);

struct VertexInput
{
    [[vk::location(0)]] float2 unitPos : UNIT_POS;           // [0,1] x [0,1]
    [[vk::location(1)]] float4 rect : RECT;                  // x, y, w, h (pixel space)
    [[vk::location(2)]] float4 srcUV : SRC_UV;               // u, v, w, h (normalized texture coords)
    [[vk::location(3)]] uint tint : TINT;                    // ARGB packed
    [[vk::location(4)]] uint clipIndex : CLIP_INDEX;
    [[vk::location(5)]] float rotation : ROTATION;           // radians, rotation about the rect's center
    [[vk::location(6)]] float4 cornerRadius : CORNER_RADIUS; // tl, tr, br, bl
};

struct Varyings
{
    float4 position : SV_Position;
    float2 pixelPos : PIXEL_POS;
    float2 uv : UV;
    // Where the fragment is from the image's center in its own unrotated frame, and half its size.
    float2 local : LOCAL;
    nointerpolation float2 halfSize : HALF_SIZE;
    // The image's own texels, which a grown edge is clamped to; and whether its edges fade at all: only
    // when the quad grew, so an image on the grid, or rotated, is drawn exactly as before.
    nointerpolation float4 srcUV : SRC_UV;
    nointerpolation float fadesEdges : FADES_EDGES;
    nointerpolation uint tint : TINT;
    nointerpolation uint clipIndex : CLIP_INDEX;
    nointerpolation float4 cornerRadius : CORNER_RADIUS;
};

[shader("vertex")]
Varyings vertexMain(VertexInput input)
{
    // An unrotated image a transform left between device pixels grows a pixel on that axis, so the
    // fragment shader can fade the pixels its edge partly covers; one on the grid stays as it was.
    float4 metrics = Globals.canvasMetrics;
    float2 devicePerLogical = metrics.zw / metrics.xy;
    float2 low = input.rect.xy * devicePerLogical;
    float2 high = (input.rect.xy + input.rect.zw) * devicePerLogical;
    float2 offGrid = max(abs(low - round(low)), abs(high - round(high)));
    bool growable = input.rotation == 0.0 && input.rect.z > 0.0 && input.rect.w > 0.0;
    float2 outset = growable ? step(1.0 / 256.0, offGrid) / devicePerLogical : float2(0.0);
    float2 unit = (input.unitPos * (input.rect.zw + 2.0 * outset) - outset) / max(input.rect.zw, float2(1e-6));

    float2 center = input.rect.xy + input.rect.zw * 0.5;
    float2 local = (unit - float2(0.5)) * input.rect.zw;
    float cs = cos(input.rotation);
    float sn = sin(input.rotation);
    float2 rotated = float2(cs * local.x - sn * local.y, sn * local.x + cs * local.y);
    float2 pixelPos = center + rotated;

    Varyings o;
    o.position = project(pixelPos);
    o.pixelPos = pixelPos;
    // Past the image's edge the texture clamps, so the grown pixels show its edge texels, faded.
    o.uv = float2(
        input.srcUV.x + unit.x * input.srcUV.z,
        input.srcUV.y + unit.y * input.srcUV.w);
    o.local = local;
    o.halfSize = input.rect.zw * 0.5;
    o.srcUV = input.srcUV;
    o.fadesEdges = outset.x > 0.0 || outset.y > 0.0 ? 1.0 : 0.0;
    o.tint = input.tint;
    o.clipIndex = input.clipIndex;
    o.cornerRadius = min(input.cornerRadius, float4(min(input.rect.z, input.rect.w) * 0.5));
    return o;
}

[shader("fragment")]
float4 fragmentMain(Varyings v) : SV_Target
{
    float clipCover = clipCoverage(v.pixelPos, v.clipIndex);
    if (outsideClipRect(v.pixelPos, v.clipIndex)) discard;
    if (clipCover <= 0.0) discard;

    float4 metrics = Globals.canvasMetrics;
    float2 devicePerLogical = metrics.zw / metrics.xy;

    // An edge between device pixels fades the pixels it partly covers, by how much it covers them.
    float coverage = 1.0;
    if (v.fadesEdges > 0.0)
    {
        float2 inside = (v.halfSize - abs(v.local)) * devicePerLogical;
        coverage = clamp(inside.x + 0.5, 0.0, 1.0) * clamp(inside.y + 0.5, 0.0, 1.0);
        if (coverage <= 0.0) discard;
    }

    // Rounded corners cut the image to their shape, smoothing its edge there over a device pixel. The
    // canvas's y runs up, so a positive local y is the image's upper half.
    float radius = v.local.y > 0.0
        ? (v.local.x > 0.0 ? v.cornerRadius.y : v.cornerRadius.x)
        : (v.local.x > 0.0 ? v.cornerRadius.z : v.cornerRadius.w);
    if (radius > 0.0)
    {
        float2 q = abs(v.local) - (v.halfSize - float2(radius));
        float distance = length(max(q, float2(0.0))) + min(max(q.x, q.y), 0.0) - radius;
        coverage *= clamp(0.5 - distance * devicePerLogical.x, 0.0, 1.0);
        if (coverage <= 0.0) discard;
    }

    // srcUV's size is negative for an image drawn flipped, so clamp between its two ends whichever way.
    float2 uvEnd = v.srcUV.xy + v.srcUV.zw;
    float2 uv = clamp(v.uv, min(v.srcUV.xy, uvEnd), max(v.srcUV.xy, uvEnd));
    float4 sample0 = u_texture.Sample(uv);
    float4 tint = unpackARGB(v.tint);
    return sample0 * tint * float4(1.0, 1.0, 1.0, coverage * clipCover);
}
