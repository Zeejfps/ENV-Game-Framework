#version 410

// Drop-shadow fragment shader for rounded rectangles. Uses Evan Wallace's
// erf-integral approximation (madebyevan.com/shaders/fast-rounded-rectangle-shadows).
// The shadowRect varying is the source rect AFTER offset/spread have been
// applied on the CPU; this shader handles only blur + clipping.

in vec2 v_pixelPos;
in vec4 v_shadowRect;
in vec4 v_borderRadius;
flat in float v_sigma;
flat in uint v_color;
flat in uint v_clipIndex;

layout(std140) uniform ClipRects {
    vec4 u_clipRects[256]; // (left, bottom, right, top)
};

out vec4 f_Color;

vec4 unpackARGB(uint c) {
    float a = float((c >> 24) & 0xFFu) / 255.0;
    float r = float((c >> 16) & 0xFFu) / 255.0;
    float g = float((c >>  8) & 0xFFu) / 255.0;
    float b = float( c        & 0xFFu) / 255.0;
    return vec4(r, g, b, a);
}

vec2 erf2(vec2 x) {
    vec2 s = sign(x);
    vec2 a = abs(x);
    vec2 v = 1.0 + (0.278393 + (0.230389 + 0.078108 * (a * a)) * a) * a;
    v *= v;
    return s - s / (v * v);
}

float gaussian1D(float x, float sigma) {
    const float pi = 3.141592653589793;
    return exp(-(x * x) / (2.0 * sigma * sigma)) / (sqrt(2.0 * pi) * sigma);
}

float roundedBoxShadowX(float x, float y, float sigma, float corner, vec2 halfSize) {
    float delta = min(halfSize.y - corner - abs(y), 0.0);
    float curved = halfSize.x - corner + sqrt(max(0.0, corner * corner - delta * delta));
    vec2 integral = 0.5 + 0.5 * erf2(vec2(x - curved, x + curved) * (sqrt(0.5) / sigma));
    return integral.y - integral.x;
}

float roundedBoxShadow(vec2 lower, vec2 upper, vec2 point, float sigma, float corner) {
    vec2 center = (lower + upper) * 0.5;
    vec2 halfSize = (upper - lower) * 0.5;
    vec2 p = point - center;

    float low  = p.y - halfSize.y;
    float high = p.y + halfSize.y;
    float start = clamp(-3.0 * sigma, low, high);
    float end   = clamp( 3.0 * sigma, low, high);

    float stepSize = (end - start) / 4.0;
    float y = start + stepSize * 0.5;
    float value = 0.0;
    for (int i = 0; i < 4; i++) {
        value += roundedBoxShadowX(p.x, p.y - y, sigma, corner, halfSize) * gaussian1D(y, sigma) * stepSize;
        y += stepSize;
    }
    return value;
}

void main() {
    vec4 clip = u_clipRects[v_clipIndex];
    if (v_pixelPos.x < clip.x || v_pixelPos.x >= clip.z ||
        v_pixelPos.y < clip.y || v_pixelPos.y >= clip.w) {
        discard;
    }

    vec2 lower = v_shadowRect.xy;
    vec2 upper = lower + v_shadowRect.zw;

    float corner = max(max(v_borderRadius.x, v_borderRadius.y),
                       max(v_borderRadius.z, v_borderRadius.w));
    float maxCorner = min(v_shadowRect.z, v_shadowRect.w) * 0.5;
    corner = min(corner, maxCorner);

    float mass = roundedBoxShadow(lower, upper, v_pixelPos, v_sigma, corner);
    vec4 rgba = unpackARGB(v_color);
    f_Color = vec4(rgba.rgb, rgba.a * clamp(mass, 0.0, 1.0));
}
