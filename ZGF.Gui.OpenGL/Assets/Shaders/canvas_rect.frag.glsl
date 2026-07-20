#version 410

in vec2 v_pixelPos;
in vec2 v_localPos;
in vec4 v_rectSize;
in vec4 v_borderRadius;
in vec4 v_borderSize;
flat in uint v_bgColor;
flat in uint v_borderColorTop;
flat in uint v_borderColorRight;
flat in uint v_borderColorBottom;
flat in uint v_borderColorLeft;
flat in uint v_clipIndex;

// Binding point is assigned from C# via glUniformBlockBinding because the
// `binding = N` layout qualifier requires GLSL 420 / ARB_shading_language_420pack,
// which is not available on macOS (capped at 4.1 / GLSL 410).
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

void main() {
    // --- Clip test ---
    vec4 clip = u_clipRects[v_clipIndex];
    if (v_pixelPos.x < clip.x || v_pixelPos.x >= clip.z ||
        v_pixelPos.y < clip.y || v_pixelPos.y >= clip.w) {
        discard;
    }

    float rectW = v_rectSize.x;
    float rectH = v_rectSize.y;
    float halfW = rectW * 0.5;
    float halfH = rectH * 0.5;

    vec2 mirror = abs(v_localPos - vec2(halfW, halfH)); // 0 at center, halfX at edges

    // Pick the radius for this quadrant.
    // borderRadius = (tl, tr, br, bl). v_localPos.y > halfH means upper, .x > halfW means right.
    bool right = v_localPos.x > halfW;
    bool top   = v_localPos.y > halfH;
    float radius =
        top  ? (right ? v_borderRadius.y : v_borderRadius.x)   // tr : tl
             : (right ? v_borderRadius.z : v_borderRadius.w);  // br : bl

    // Border thickness for the active quadrant (used for rounded-corner pivot test).
    float borderW = right ? v_borderSize.y : v_borderSize.w;
    float borderH = top   ? v_borderSize.x : v_borderSize.z;

    vec2 pivot = vec2(halfW - radius, halfH - radius);
    bool inCornerZone = (mirror.x > pivot.x) && (mirror.y > pivot.y);

    // Distance from the corner pivot and its screen-space derivative, computed
    // unconditionally so fwidth has valid neighbours regardless of branch.
    float d = length(mirror - pivot);
    float aa = max(fwidth(d), 1e-6);

    float coverage = 1.0;
    bool isFill;
    if (inCornerZone && radius > 0.0) {
        // Soft outer edge of the rounded corner: 1 inside, 0 outside, ramped over one pixel.
        coverage = clamp((radius - d) / aa + 0.5, 0.0, 1.0);
        if (coverage <= 0.0) discard;
        // Inside the rounded corner: fill if within the inner ellipse.
        if (borderH < radius && borderW < radius) {
            float ix = (mirror.x - pivot.x) / max(radius - borderW, 1e-6);
            float iy = (mirror.y - pivot.y) / max(radius - borderH, 1e-6);
            isFill = (ix * ix + iy * iy) <= 1.0;
        } else {
            isFill = false;
        }
    } else {
        // Straight-edge zone.
        bool insideX = mirror.x < (halfW - borderW);
        bool insideY = mirror.y < (halfH - borderH);
        isFill = insideX && insideY;
    }

    if (isFill) {
        f_Color = unpackARGB(v_bgColor);
        f_Color.a *= coverage;
        return;
    }

    // We are in a border. Pick which side wins.
    // Priority follows the software canvas painting order: bottom > top > right > left.
    bool inBottom = v_localPos.y < v_borderSize.z;
    bool inTop    = v_localPos.y >= rectH - v_borderSize.x;
    bool inRight  = v_localPos.x >= rectW - v_borderSize.y;
    bool inLeft   = v_localPos.x < v_borderSize.w;

    uint pickedColor;
    if (inBottom)      pickedColor = v_borderColorBottom;
    else if (inTop)    pickedColor = v_borderColorTop;
    else if (inRight)  pickedColor = v_borderColorRight;
    else if (inLeft)   pickedColor = v_borderColorLeft;
    else {
        // We are in the rounded outer ring of a corner. Use the dominant side
        // (top or bottom > right/left), matching software priority at corners.
        if (top)   pickedColor = v_borderColorTop;
        else       pickedColor = v_borderColorBottom;
    }

    f_Color = unpackARGB(pickedColor);
    f_Color.a *= coverage;
}
