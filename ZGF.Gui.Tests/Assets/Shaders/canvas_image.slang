// Canvas image instance shader. Renders an instanced unit-quad as a textured
// image, optionally tinted by per-instance color.

struct VertexInput
{
    [[vk::location(0)]] float2 unitPos    : POSITION;
    [[vk::location(1)]] float4 rect       : TEXCOORD0;
    [[vk::location(2)]] float4 srcUV      : TEXCOORD1;
    [[vk::location(3)]] uint   tint       : COLOR0;
    [[vk::location(4)]] uint   clipIndex  : COLOR1;
    [[vk::location(5)]] float  rotation   : TEXCOORD2; // radians, rotation about the rect's center
};

struct Varyings
{
    float4 position : SV_Position;
    float2 pixelPos : TEXCOORD0;
    float2 uv       : TEXCOORD1;
    nointerpolation uint tint : COLOR0;
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

Sampler2D u_texture;

[shader("vertex")]
Varyings vertexMain(VertexInput input)
{
    Varyings o;
    float2 center = input.rect.xy + input.rect.zw * 0.5;
    float2 local = (input.unitPos - float2(0.5, 0.5)) * input.rect.zw;
    float cs = cos(input.rotation);
    float sn = sin(input.rotation);
    float2 rotated = float2(cs * local.x - sn * local.y, sn * local.x + cs * local.y);
    float2 pixelPos = center + rotated;
    o.position = mul(u_projection, float4(pixelPos, 0.0, 1.0));
    o.pixelPos = pixelPos;
    o.uv = float2(
        input.srcUV.x + input.unitPos.x * input.srcUV.z,
        input.srcUV.y + input.unitPos.y * input.srcUV.w);
    o.tint = input.tint;
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

    float4 sample0 = u_texture.Sample(v.uv);
    float4 tint = unpackARGB(v.tint);
    return sample0 * tint;
}
