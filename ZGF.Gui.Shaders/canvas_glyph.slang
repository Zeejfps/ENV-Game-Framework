#include "canvas_common.slangh"

[[vk::binding(2)]] Sampler2D u_atlas : register(t0);

struct VertexInput
{
    [[vk::location(0)]] float2 unitPos : UNIT_POS;  // [0,1] x [0,1]
    [[vk::location(1)]] float4 rect : RECT;         // x, y, w, h (pixel space)
    [[vk::location(2)]] float4 atlasUV : ATLAS_UV;  // u, v, w, h (normalized atlas coords)
    [[vk::location(3)]] uint color : COLOR;         // ARGB packed
    [[vk::location(4)]] uint clipIndex : CLIP_INDEX;
    [[vk::location(5)]] float rotation : ROTATION;  // radians, rotation about the glyph rect's center
};

struct Varyings
{
    float4 position : SV_Position;
    float2 pixelPos : PIXEL_POS;
    float2 atlasUV : ATLAS_UV;
    nointerpolation uint color : COLOR;
    nointerpolation uint clipIndex : CLIP_INDEX;
};

[shader("vertex")]
Varyings vertexMain(VertexInput input)
{
    float2 center = input.rect.xy + input.rect.zw * 0.5;
    float2 local = (input.unitPos - float2(0.5)) * input.rect.zw;
    float cs = cos(input.rotation);
    float sn = sin(input.rotation);
    float2 rotated = float2(cs * local.x - sn * local.y, sn * local.x + cs * local.y);
    float2 pixelPos = center + rotated;

    Varyings o;
    o.position = project(pixelPos);
    o.pixelPos = pixelPos;
    // Atlas is uploaded with Y already flipped, so the texture's V-axis matches the
    // pixel-space Y-up axis. Bottom of dest rect (unit y=0) samples bottom of glyph in atlas.
    o.atlasUV = float2(
        input.atlasUV.x + input.unitPos.x * input.atlasUV.z,
        input.atlasUV.y + input.unitPos.y * input.atlasUV.w);
    o.color = input.color;
    o.clipIndex = input.clipIndex;
    return o;
}

[shader("fragment")]
float4 fragmentMain(Varyings v) : SV_Target
{
    float clipCover = clipCoverage(v.pixelPos, v.clipIndex);
    if (outsideClipRect(v.pixelPos, v.clipIndex)) discard;
    if (clipCover <= 0.0) discard;

    float alpha = u_atlas.Sample(v.atlasUV).r;
    if (alpha <= 0.0) discard;

    float4 color = unpackARGB(v.color);
    return float4(color.rgb, color.a * alpha * clipCover);
}
