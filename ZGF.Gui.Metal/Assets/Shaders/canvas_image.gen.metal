#include <metal_stdlib>
using namespace metal;

// The Metal port of canvas_image.vert.glsl and canvas_image.frag.glsl; keep the two in step.

struct Globals
{
    float4x4 u_projection;
    // Logical canvas size followed by the actual, rounded framebuffer size.
    float4 u_canvasMetrics;
};

struct ClipRects
{
    array<float4, 256> u_clipRects;
    array<float4, 256> u_clipRounds;
    array<float4, 256> u_clipRadii;
};

struct VertexInput
{
    float2 unitPos [[attribute(0)]];      // [0,1] x [0,1]
    float4 rect [[attribute(1)]];         // x, y, w, h (pixel space)
    float4 srcUV [[attribute(2)]];        // u, v, w, h (normalized texture coords)
    uint tint [[attribute(3)]];           // ARGB packed
    uint clipIndex [[attribute(4)]];
    float rotation [[attribute(5)]];      // radians, rotation about the rect's center
    float4 cornerRadius [[attribute(6)]]; // tl, tr, br, bl
};

struct Varyings
{
    float4 position [[position]];
    float2 pixelPos;
    float2 uv;
    // Where the fragment is from the image's center in its own unrotated frame, and half its size.
    float2 local;
    float2 halfSize [[flat]];
    // The image's own texels, which a grown edge is clamped to; and whether its edges fade at all — only
    // when the quad grew, so an image on the grid, or rotated, is drawn exactly as before.
    float4 srcUV [[flat]];
    float fadesEdges [[flat]];
    uint tint [[flat]];
    uint clipIndex [[flat]];
    float4 cornerRadius [[flat]];
};

static float4 unpackARGB(uint c)
{
    return float4(float((c >> 16) & 0xFFu), float((c >> 8) & 0xFFu), float(c & 0xFFu), float((c >> 24) & 0xFFu)) / 255.0;
}

[[vertex]] Varyings vertexMain(VertexInput in [[stage_in]], constant Globals& globals [[buffer(0)]])
{
    // An unrotated image a transform left between device pixels grows a pixel on that axis, so the
    // fragment stage can fade the pixels its edge partly covers; one on the grid stays as it was.
    float2 devicePerLogical = globals.u_canvasMetrics.zw / globals.u_canvasMetrics.xy;
    float2 low = in.rect.xy * devicePerLogical;
    float2 high = (in.rect.xy + in.rect.zw) * devicePerLogical;
    float2 offGrid = max(abs(low - round(low)), abs(high - round(high)));
    bool growable = in.rotation == 0.0 && in.rect.z > 0.0 && in.rect.w > 0.0;
    float2 outset = growable ? step(1.0 / 256.0, offGrid) / devicePerLogical : float2(0.0);
    float2 unit = (in.unitPos * (in.rect.zw + 2.0 * outset) - outset) / max(in.rect.zw, float2(1e-6));

    float2 center = in.rect.xy + in.rect.zw * 0.5;
    float2 local = (unit - float2(0.5)) * in.rect.zw;
    float cs = cos(in.rotation);
    float sn = sin(in.rotation);
    float2 rotated = float2(cs * local.x - sn * local.y, sn * local.x + cs * local.y);
    float2 pixelPos = center + rotated;

    Varyings out;
    out.position = float4(pixelPos, 0.0, 1.0) * globals.u_projection;
    out.pixelPos = pixelPos;
    // Past the image's edge the texture clamps, so the grown pixels show its edge texels, faded.
    out.uv = in.srcUV.xy + unit * in.srcUV.zw;
    out.local = local;
    out.halfSize = in.rect.zw * 0.5;
    out.srcUV = in.srcUV;
    out.fadesEdges = outset.x > 0.0 || outset.y > 0.0 ? 1.0 : 0.0;
    out.tint = in.tint;
    out.clipIndex = in.clipIndex;
    out.cornerRadius = min(in.cornerRadius, float4(min(in.rect.z, in.rect.w) * 0.5));
    return out;
}

// How much of the pixel at p the clip's rounded rect covers: 1 inside it, 0 outside, and between over the
// device pixel, pixel wide, its edge crosses. Always 1 for a clip that is not rounded. Written by hand, as the
// twin of clipCoverage in the GLSL shaders.
static float clipCoverage(float2 p, float4 rounded, float r, float pixel)
{
    if (r <= 0.0)
    {
        return 1.0;
    }
    float2 halfSize = (rounded.zw - rounded.xy) * 0.5;
    float2 q = abs(p - (rounded.xy + rounded.zw) * 0.5) - (halfSize - float2(r));
    float d = length(max(q, float2(0.0))) + min(max(q.x, q.y), 0.0) - r;
    return clamp(0.5 - d / pixel, 0.0, 1.0);
}

[[fragment]] float4 fragmentMain(
    Varyings in [[stage_in]],
    constant Globals& globals [[buffer(0)]],
    constant ClipRects& clipRects [[buffer(1)]],
    texture2d<float, access::sample> u_texture [[texture(0)]],
    sampler u_sampler [[sampler(0)]])
{
    float clipCover_0 = clipCoverage(in.pixelPos, clipRects.u_clipRounds[in.clipIndex], clipRects.u_clipRadii[in.clipIndex].x, max(fwidth(in.pixelPos.x), 0.0001));
    if (clipCover_0 <= 0.0)
    {
        discard_fragment();
    }
    float4 clip = clipRects.u_clipRects[in.clipIndex];
    if (in.pixelPos.x < clip.x || in.pixelPos.x >= clip.z ||
        in.pixelPos.y < clip.y || in.pixelPos.y >= clip.w)
    {
        discard_fragment();
    }

    float2 devicePerLogical = globals.u_canvasMetrics.zw / globals.u_canvasMetrics.xy;

    // An edge between device pixels fades the pixels it partly covers, by how much it covers them.
    float coverage = 1.0;
    if (in.fadesEdges > 0.0)
    {
        float2 inside = (in.halfSize - abs(in.local)) * devicePerLogical;
        coverage = clamp(inside.x + 0.5, 0.0, 1.0) * clamp(inside.y + 0.5, 0.0, 1.0);
        if (coverage <= 0.0) discard_fragment();
    }

    // Rounded corners cut the image to their shape, smoothing its edge there over a device pixel. The
    // canvas's y runs up, so a positive local y is the image's upper half.
    float radius = in.local.y > 0.0
        ? (in.local.x > 0.0 ? in.cornerRadius.y : in.cornerRadius.x)
        : (in.local.x > 0.0 ? in.cornerRadius.z : in.cornerRadius.w);
    if (radius > 0.0)
    {
        float2 q = abs(in.local) - (in.halfSize - float2(radius));
        float distance = length(max(q, float2(0.0))) + min(max(q.x, q.y), 0.0) - radius;
        coverage *= clamp(0.5 - distance * devicePerLogical.x, 0.0, 1.0);
        if (coverage <= 0.0) discard_fragment();
    }

    // srcUV's size is negative for an image drawn flipped, so clamp between its two ends whichever way.
    float2 uvEnd = in.srcUV.xy + in.srcUV.zw;
    float2 uv = clamp(in.uv, min(in.srcUV.xy, uvEnd), max(in.srcUV.xy, uvEnd));
    float4 sample0 = u_texture.sample(u_sampler, uv);
    return sample0 * unpackARGB(in.tint) * float4(1.0, 1.0, 1.0, coverage * clipCover_0);
}
