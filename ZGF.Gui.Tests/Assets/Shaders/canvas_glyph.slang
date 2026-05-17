// Canvas glyph instance shader. Renders an instanced unit-quad as a glyph
// sampled from a single-channel font atlas, tinted by per-instance color.

struct VertexInput
{
    [[vk::location(0)]] float2 unitPos    : POSITION;
    [[vk::location(1)]] float4 rect       : TEXCOORD0;  // x, y, w, h (pixel space)
    [[vk::location(2)]] float4 atlasUV    : TEXCOORD1;  // u, v, w, h (normalized)
    [[vk::location(3)]] uint   color      : COLOR0;     // ARGB packed
    [[vk::location(4)]] uint   clipIndex  : COLOR1;
};

struct Varyings
{
    float4 position : SV_Position;
    float2 pixelPos : TEXCOORD0;
    float2 atlasUV  : TEXCOORD1;
    nointerpolation uint color : COLOR0;
    nointerpolation uint clipIndex : COLOR1;
};

cbuffer Globals
{
    float4x4 u_projection;
}

cbuffer ClipRects
{
    float4 u_clipRects[256];
}

Sampler2D u_atlas;

[shader("vertex")]
Varyings vertexMain(VertexInput input)
{
    Varyings o;
    float2 pixelPos = input.rect.xy + input.unitPos * input.rect.zw;
    o.position = mul(u_projection, float4(pixelPos, 0.0, 1.0));
    o.pixelPos = pixelPos;
    o.atlasUV = float2(
        input.atlasUV.x + input.unitPos.x * input.atlasUV.z,
        input.atlasUV.y + input.unitPos.y * input.atlasUV.w);
    o.color = input.color;
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

[shader("fragment")]
float4 fragmentMain(Varyings v) : SV_Target
{
    float4 clip = u_clipRects[v.clipIndex];
    if (v.pixelPos.x < clip.x || v.pixelPos.x >= clip.z ||
        v.pixelPos.y < clip.y || v.pixelPos.y >= clip.w)
    {
        discard;
    }

    float alpha = u_atlas.Sample(v.atlasUV).r;
    if (alpha <= 0.0) discard;

    float4 color = unpackARGB(v.color);
    return float4(color.rgb, color.a * alpha);
}
