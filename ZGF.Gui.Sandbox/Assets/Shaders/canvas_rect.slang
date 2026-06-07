// Canvas rectangle instance shader. Renders an instanced unit-quad as a
// rounded-corner rectangle with per-side borders, clipped by a global clip
// table.
//
// Cross-target: Slang emits glsl_450 (with binding qualifiers) for the GL
// backend and Metal Shading Language for the Metal backend.

struct VertexInput
{
    [[vk::location(0)]] float2 unitPos          : POSITION;
    [[vk::location(1)]] float4 rect             : TEXCOORD0;  // x, y, w, h (pixel space)
    [[vk::location(2)]] float4 borderRadius     : TEXCOORD1;  // tl, tr, br, bl
    [[vk::location(3)]] float4 borderSize       : TEXCOORD2;  // top, right, bottom, left
    [[vk::location(4)]] uint   bgColor          : COLOR0;     // ARGB packed
    [[vk::location(5)]] uint   borderColorTop   : COLOR1;
    [[vk::location(6)]] uint   borderColorRight : COLOR2;
    [[vk::location(7)]] uint   borderColorBottom: COLOR3;
    [[vk::location(8)]] uint   borderColorLeft  : COLOR4;
    [[vk::location(9)]] uint   clipIndex        : COLOR5;
};

struct Varyings
{
    float4 position : SV_Position;
    float2 pixelPos : TEXCOORD0;
    float2 localPos : TEXCOORD1;
    float4 rectSize : TEXCOORD2;
    float4 borderRadius : TEXCOORD3;
    float4 borderSize : TEXCOORD4;
    nointerpolation uint bgColor : COLOR0;
    nointerpolation uint borderColorTop : COLOR1;
    nointerpolation uint borderColorRight : COLOR2;
    nointerpolation uint borderColorBottom : COLOR3;
    nointerpolation uint borderColorLeft : COLOR4;
    nointerpolation uint clipIndex : COLOR5;
};

cbuffer Globals
{
    float4x4 u_projection;
}

cbuffer ClipRects
{
    float4 u_clipRects[256]; // (left, bottom, right, top)
}

[shader("vertex")]
Varyings vertexMain(VertexInput input)
{
    Varyings o;
    float2 pixelPos = input.rect.xy + input.unitPos * input.rect.zw;
    o.position = mul(u_projection, float4(pixelPos, 0.0, 1.0));
    o.pixelPos = pixelPos;
    o.localPos = input.unitPos * input.rect.zw;
    o.rectSize = float4(input.rect.zw, input.rect.zw);
    o.borderRadius = input.borderRadius;
    o.borderSize = input.borderSize;
    o.bgColor = input.bgColor;
    o.borderColorTop = input.borderColorTop;
    o.borderColorRight = input.borderColorRight;
    o.borderColorBottom = input.borderColorBottom;
    o.borderColorLeft = input.borderColorLeft;
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
    // --- Clip test ---
    float4 clip = u_clipRects[v.clipIndex];
    if (v.pixelPos.x < clip.x || v.pixelPos.x >= clip.z ||
        v.pixelPos.y < clip.y || v.pixelPos.y >= clip.w)
    {
        discard;
    }

    float rectW = v.rectSize.x;
    float rectH = v.rectSize.y;
    float halfW = rectW * 0.5;
    float halfH = rectH * 0.5;

    float2 mirror = abs(v.localPos - float2(halfW, halfH));

    bool right = v.localPos.x > halfW;
    bool top   = v.localPos.y > halfH;
    float radius =
        top  ? (right ? v.borderRadius.y : v.borderRadius.x)
             : (right ? v.borderRadius.z : v.borderRadius.w);

    float borderW = right ? v.borderSize.y : v.borderSize.w;
    float borderH = top   ? v.borderSize.x : v.borderSize.z;

    float2 pivot = float2(halfW - radius, halfH - radius);
    bool inCornerZone = (mirror.x > pivot.x) && (mirror.y > pivot.y);

    // Distance from the corner pivot and its screen-space derivative, computed
    // unconditionally so fwidth has valid neighbours regardless of branch.
    float d = length(mirror - pivot);
    float aa = max(fwidth(d), 1e-6);

    float coverage = 1.0;
    bool isFill;
    if (inCornerZone && radius > 0.0)
    {
        // Soft outer edge of the rounded corner: 1 inside, 0 outside, ramped over one pixel.
        coverage = clamp((radius - d) / aa + 0.5, 0.0, 1.0);
        if (coverage <= 0.0) discard;
        if (borderH < radius && borderW < radius)
        {
            float ix = (mirror.x - pivot.x) / max(radius - borderW, 1e-6);
            float iy = (mirror.y - pivot.y) / max(radius - borderH, 1e-6);
            isFill = (ix * ix + iy * iy) <= 1.0;
        }
        else
        {
            isFill = false;
        }
    }
    else
    {
        bool insideX = mirror.x < (halfW - borderW);
        bool insideY = mirror.y < (halfH - borderH);
        isFill = insideX && insideY;
    }

    if (isFill)
    {
        float4 fillColor = unpackARGB(v.bgColor);
        fillColor.a *= coverage;
        return fillColor;
    }

    // Border side priority: bottom > top > right > left (matches software canvas).
    bool inBottom = v.localPos.y < v.borderSize.z;
    bool inTop    = v.localPos.y >= rectH - v.borderSize.x;
    bool inRight  = v.localPos.x >= rectW - v.borderSize.y;
    bool inLeft   = v.localPos.x < v.borderSize.w;

    uint pickedColor;
    if (inBottom)      pickedColor = v.borderColorBottom;
    else if (inTop)    pickedColor = v.borderColorTop;
    else if (inRight)  pickedColor = v.borderColorRight;
    else if (inLeft)   pickedColor = v.borderColorLeft;
    else
    {
        if (top) pickedColor = v.borderColorTop;
        else     pickedColor = v.borderColorBottom;
    }

    float4 borderColor = unpackARGB(pickedColor);
    borderColor.a *= coverage;
    return borderColor;
}
