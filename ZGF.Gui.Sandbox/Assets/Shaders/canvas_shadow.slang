// Drop-shadow shader for rounded rectangles. Renders a Gaussian-blurred
// silhouette of an axis-aligned rounded rectangle using the closed-form
// erf integral approximation from Evan Wallace's "Fast Rounded Rectangle
// Shadows" (madebyevan.com/shaders/fast-rounded-rectangle-shadows). The
// inner shadow rect is supplied already shifted by the offset and grown
// by spread; this shader handles only blur and clipping.
//
// The vertex stage expands the unit quad into the outer (penumbra-inclusive)
// quad. The fragment stage samples the rounded-box shadow at the pixel's
// world position.
//
// Cross-target: Slang emits glsl_450 for the GL backend and Metal Shading
// Language for the Metal backend. The OpenGL backend currently consumes the
// hand-written canvas_shadow.{vert,frag}.glsl in this directory instead of
// canvas_shadow.gen.glsl, matching the rest of the canvas pipeline.

struct VertexInput
{
    [[vk::location(0)]] float2 unitPos       : POSITION;
    [[vk::location(1)]] float4 outerRect     : TEXCOORD0;   // x, y, w, h in pixel space (drawn quad)
    [[vk::location(2)]] float4 shadowRect    : TEXCOORD1;   // x, y, w, h in pixel space (source rect post offset/spread)
    [[vk::location(3)]] float4 borderRadius  : TEXCOORD2;   // tl, tr, br, bl
    [[vk::location(4)]] float  sigma         : TEXCOORD3;
    [[vk::location(5)]] uint   color         : COLOR0;      // ARGB packed
    [[vk::location(6)]] uint   clipIndex     : COLOR1;
};

struct Varyings
{
    float4 position : SV_Position;
    float2 pixelPos : TEXCOORD0;
    float4 shadowRect : TEXCOORD1;
    float4 borderRadius : TEXCOORD2;
    nointerpolation float sigma : TEXCOORD3;
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

[shader("vertex")]
Varyings vertexMain(VertexInput input)
{
    Varyings o;
    float2 pixelPos = input.outerRect.xy + input.unitPos * input.outerRect.zw;
    o.position = mul(u_projection, float4(pixelPos, 0.0, 1.0));
    o.pixelPos = pixelPos;
    o.shadowRect = input.shadowRect;
    o.borderRadius = input.borderRadius;
    o.sigma = input.sigma;
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

// Cheap erf approximation (Abramowitz/Stegun fitted form used by Evan Wallace).
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

// 1D box integral along x at a vertical offset y from the box center.
// halfSize is half the box's (width, height); corner is the corner radius.
float roundedBoxShadowX(float x, float y, float sigma, float corner, float2 halfSize)
{
    float delta = min(halfSize.y - corner - abs(y), 0.0);
    float curved = halfSize.x - corner + sqrt(max(0.0, corner * corner - delta * delta));
    float2 integral = 0.5 + 0.5 * erf2(float2(x - curved, x + curved) * (sqrt(0.5) / sigma));
    return integral.y - integral.x;
}

// 2D rounded-box shadow mass at the given point (numerical y integration).
float roundedBoxShadow(float2 lower, float2 upper, float2 point, float sigma, float corner)
{
    float2 center = (lower + upper) * 0.5;
    float2 halfSize = (upper - lower) * 0.5;
    float2 p = point - center;

    float low = p.y - halfSize.y;
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
    float4 clip = u_clipRects[v.clipIndex];
    if (v.pixelPos.x < clip.x || v.pixelPos.x >= clip.z ||
        v.pixelPos.y < clip.y || v.pixelPos.y >= clip.w)
    {
        discard;
    }

    float2 lower = v.shadowRect.xy;
    float2 upper = lower + v.shadowRect.zw;

    // Single corner radius for the shadow approximation: use the largest of the
    // four so the silhouette never under-rounds; clamp to half the smaller side
    // so the math stays well-defined.
    float corner = max(max(v.borderRadius.x, v.borderRadius.y),
                       max(v.borderRadius.z, v.borderRadius.w));
    float maxCorner = min(v.shadowRect.z, v.shadowRect.w) * 0.5;
    corner = min(corner, maxCorner);

    float mass = roundedBoxShadow(lower, upper, v.pixelPos, v.sigma, corner);
    float4 rgba = unpackARGB(v.color);
    return float4(rgba.rgb, rgba.a * clamp(mass, 0.0, 1.0));
}
