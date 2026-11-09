#include "canvas_common.slangh"

struct VertexInput
{
    [[vk::location(0)]] float2 unitPos : UNIT_POS;           // [0,1] x [0,1]
    [[vk::location(1)]] float4 rect : RECT;                  // x, y, w, h (pixel space)
    [[vk::location(2)]] float4 borderRadius : BORDER_RADIUS; // tl, tr, br, bl
    [[vk::location(3)]] float4 borderSize : BORDER_SIZE;     // top, right, bottom, left
    [[vk::location(4)]] uint bgColor : BG_COLOR;             // ARGB packed
    [[vk::location(5)]] uint borderColorTop : BORDER_COLOR_TOP;
    [[vk::location(6)]] uint borderColorRight : BORDER_COLOR_RIGHT;
    [[vk::location(7)]] uint borderColorBottom : BORDER_COLOR_BOTTOM;
    [[vk::location(8)]] uint borderColorLeft : BORDER_COLOR_LEFT;
    [[vk::location(9)]] uint clipIndex : CLIP_INDEX;
};

struct Varyings
{
    float4 position : SV_Position;
    nointerpolation float2 rectOrigin : RECT_ORIGIN;
    nointerpolation float4 rectSize : RECT_SIZE; // (w, h, w, h) for convenience
    nointerpolation float4 borderRadius : BORDER_RADIUS;
    nointerpolation float4 borderSize : BORDER_SIZE;
    nointerpolation uint bgColor : BG_COLOR;
    nointerpolation uint borderColorTop : BORDER_COLOR_TOP;
    nointerpolation uint borderColorRight : BORDER_COLOR_RIGHT;
    nointerpolation uint borderColorBottom : BORDER_COLOR_BOTTOM;
    nointerpolation uint borderColorLeft : BORDER_COLOR_LEFT;
    nointerpolation uint clipIndex : CLIP_INDEX;
    // Whether the rect grew to fade an edge between device pixels; a rect on the grid is drawn exactly as before.
    nointerpolation float fadesEdges : FADES_EDGES;
};

[shader("vertex")]
Varyings vertexMain(VertexInput input)
{
    // A rect moved or scaled by a transform can have an edge between device pixels. The quad grows a
    // pixel on that axis so the pixels the edge partly covers are drawn, and the fragment shader fades
    // them by how much. An edge on the grid needs neither and the quad stays as it was.
    float4 metrics = Globals.canvasMetrics;
    float2 devicePerLogical = metrics.zw / metrics.xy;
    float2 low = input.rect.xy * devicePerLogical;
    float2 high = (input.rect.xy + input.rect.zw) * devicePerLogical;
    float2 offGrid = max(abs(low - round(low)), abs(high - round(high)));
    // A rect with no area stays without one rather than growing into a faint line.
    bool hasArea = input.rect.z > 0.0 && input.rect.w > 0.0;
    float2 outset = hasArea ? step(1.0 / 256.0, offGrid) / devicePerLogical : float2(0.0);

    Varyings o;
    o.fadesEdges = outset.x > 0.0 || outset.y > 0.0 ? 1.0 : 0.0;
    float2 pixelPos = input.rect.xy - outset + input.unitPos * (input.rect.zw + 2.0 * outset);
    o.position = project(pixelPos);
    o.rectOrigin = input.rect.xy;
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

[shader("fragment")]
float4 fragmentMain(Varyings v) : SV_Target
{
    // Interpolated local coordinates can fall on opposite sides of an exact border
    // boundary along the same pixel row. Reconstruct from the fragment's pixel center
    // and flat instance data so straight borders have uniform thickness at any DPI.
    float4 metrics = Globals.canvasMetrics;
    float2 pixelPos = fragmentPixelPos(v.position);
    float2 localPos = pixelPos - v.rectOrigin;
    float clipCover = clipCoverage(pixelPos, v.clipIndex);
    // Discarded only once clipCoverage's derivative is taken: fwidth reads the 2x2 quad's neighbours,
    // which must all still run the same code for it to be defined.
    bool clippedOut = outsideClipRect(pixelPos, v.clipIndex) || clipCover <= 0.0;

    float rectW = v.rectSize.x;
    float rectH = v.rectSize.y;
    float halfW = rectW * 0.5;
    float halfH = rectH * 0.5;

    float2 mirror = abs(localPos - float2(halfW, halfH)); // 0 at center, halfX at edges

    // Pick the radius for this quadrant.
    // borderRadius = (tl, tr, br, bl). localPos.y > halfH means upper, .x > halfW means right.
    bool right = localPos.x > halfW;
    bool top   = localPos.y > halfH;
    float radius =
        top  ? (right ? v.borderRadius.y : v.borderRadius.x)   // tr : tl
             : (right ? v.borderRadius.z : v.borderRadius.w);  // br : bl

    // Border thickness for the active quadrant (used for rounded-corner pivot test).
    float borderW = right ? v.borderSize.y : v.borderSize.w;
    float borderH = top   ? v.borderSize.x : v.borderSize.z;

    float2 pivot = float2(halfW - radius, halfH - radius);
    bool inCornerZone = (mirror.x > pivot.x) && (mirror.y > pivot.y);

    // A rounded corner's two edges ramp over one device pixel, each by how far its value moves across one,
    // worked out from the geometry rather than taken with fwidth: the compiler is free to compute a value
    // only inside the branch that uses it, so a 2x2 quad straddling where the corner starts read undefined
    // neighbours there, which left a 1-device-pixel seam down the corner at 1x, where corners start mid-quad.
    float2 logicalPerDevice = metrics.xy / metrics.zw;
    float2 fromPivot = mirror - pivot;
    float d = length(fromPivot);
    float aa = max(dot(abs(fromPivot), logicalPerDevice) / max(d, 1e-6), 1e-6);

    // The inner (fill vs border) ellipse e == 1 and how far e moves across a device pixel.
    float innerW = max(radius - borderW, 1e-6);
    float innerH = max(radius - borderH, 1e-6);
    float ix = (mirror.x - pivot.x) / innerW;
    float iy = (mirror.y - pivot.y) / innerH;
    float e = ix * ix + iy * iy;
    float aaInner = max(dot(abs(float2(2.0 * ix / innerW, 2.0 * iy / innerH)), logicalPerDevice), 1e-6);

    if (clippedOut) discard;

    // At fractional display scales an outer edge can pass through a pixel center.
    // A zero-width side must still count as fill there, not borrow another side's color.
    bool inBottom = v.borderSize.z > 0.0 && localPos.y < v.borderSize.z;
    bool inTop    = v.borderSize.x > 0.0 && localPos.y >= rectH - v.borderSize.x;
    bool inRight  = v.borderSize.y > 0.0 && localPos.x >= rectW - v.borderSize.y;
    bool inLeft   = v.borderSize.w > 0.0 && localPos.x < v.borderSize.w;

    float coverage = 1.0;   // outer silhouette (shape vs background)
    float fillFactor;       // 1 = fill, 0 = border, ramped across the inner boundary
    if (inCornerZone && radius > 0.0)
    {
        // Antialiased outer edge of the rounded corner: 1 inside, 0 outside, ramped over one pixel.
        coverage = clamp((radius - d) / aa + 0.5, 0.0, 1.0);
        if (coverage <= 0.0) discard;
        if (borderW < radius && borderH < radius)
        {
            // Antialiased INNER boundary (fill vs border): the ellipse e == 1, ramped like the outer
            // edge. Keeps a filled corner smooth even with a transparent border; a hard test would
            // leave the fill's own corner aliased (reads as square).
            fillFactor = clamp((1.0 - e) / aaInner + 0.5, 0.0, 1.0);
        }
        else
        {
            fillFactor = 0.0; // border thicker than the radius: the whole corner is border
        }
    }
    else if (v.fadesEdges > 0.0)
    {
        // Straight edges a transform left between device pixels: the outer edge and the border's inner
        // edge each fade the pixels they partly cover, so the rect moves smoothly instead of a pixel at a
        // time. A side with no border lets the fill run to the outer edge.
        float2 devicePerLogical = metrics.zw / metrics.xy;
        float2 inside = (float2(halfW, halfH) - mirror) * devicePerLogical;
        coverage = clamp(inside.x + 0.5, 0.0, 1.0) * clamp(inside.y + 0.5, 0.0, 1.0);
        if (coverage <= 0.0) discard;
        const float noBorder = 1.0e6;
        float fromLeft   = v.borderSize.w > 0.0 ? localPos.x - v.borderSize.w : noBorder;
        float fromRight  = v.borderSize.y > 0.0 ? rectW - v.borderSize.y - localPos.x : noBorder;
        float fromBottom = v.borderSize.z > 0.0 ? localPos.y - v.borderSize.z : noBorder;
        float fromTop    = v.borderSize.x > 0.0 ? rectH - v.borderSize.x - localPos.y : noBorder;
        float2 intoFill = float2(min(fromLeft, fromRight), min(fromBottom, fromTop)) * devicePerLogical;
        fillFactor = clamp(intoFill.x + 0.5, 0.0, 1.0) * clamp(intoFill.y + 0.5, 0.0, 1.0);
        // A pixel whose center is just inside the fill takes the nearest side's border color for its share.
        if (!(inBottom || inTop || inRight || inLeft))
        {
            float nearest = min(min(fromLeft, fromRight), min(fromBottom, fromTop));
            inBottom = nearest == fromBottom && v.borderSize.z > 0.0;
            inTop    = !inBottom && nearest == fromTop && v.borderSize.x > 0.0;
            inRight  = !inBottom && !inTop && nearest == fromRight && v.borderSize.y > 0.0;
            inLeft   = !inBottom && !inTop && !inRight && nearest == fromLeft && v.borderSize.w > 0.0;
        }
    }
    else
    {
        // Straight-edge zone on the pixel grid: axis-aligned, needs no antialiasing.
        fillFactor = (inBottom || inTop || inRight || inLeft) ? 0.0 : 1.0;
    }

    // Border side priority: bottom > top > right > left (matches software canvas).
    uint pickedColor;
    if (inBottom)      pickedColor = v.borderColorBottom;
    else if (inTop)    pickedColor = v.borderColorTop;
    else if (inRight)  pickedColor = v.borderColorRight;
    else if (inLeft)   pickedColor = v.borderColorLeft;
    else               pickedColor = top ? v.borderColorTop : v.borderColorBottom;

    // Composite fill over border in premultiplied space, then unpremultiply, so a transparent border
    // lets the fill's rounded corner fade correctly (a straight-alpha mix would darken the edge).
    // coverage applies the outer silhouette.
    float4 fillCol   = unpackARGB(v.bgColor);
    float4 borderCol = unpackARGB(pickedColor);
    float outA       = lerp(borderCol.a, fillCol.a, fillFactor);
    float3 outPre    = lerp(borderCol.rgb * borderCol.a, fillCol.rgb * fillCol.a, fillFactor);
    float3 outRgb    = outA > 0.0 ? outPre / outA : float3(0.0);
    return float4(outRgb, outA * coverage * clipCover);
}
