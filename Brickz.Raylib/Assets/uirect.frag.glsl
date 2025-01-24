#version 460

in vec4 uvs;
in vec4 color;
in vec4 borderSize;
in vec4 borderColor;
in vec4 borderRadius;
in vec4 rectInPixels;

out vec4 f_Color;

// A standard gaussian function, used for weighting samples
float gaussian(float x, float sigma) {
  const float pi = 3.141592653589793;
  return exp(-(x * x) / (2.0 * sigma * sigma)) / (sqrt(2.0 * pi) * sigma);
}

// This approximates the error function, needed for the gaussian integral
vec2 erf(vec2 x) {
  vec2 s = sign(x), a = abs(x);
  x = 1.0 + (0.278393 + (0.230389 + 0.078108 * (a * a)) * a) * a;
  x *= x;
  return s - s / (x * x);
}

// Return the blurred mask along the x dimension
float roundedBoxShadowX(float x, float y, float sigma, float corner, vec2 halfSize) {
  float delta = min(halfSize.y - corner - abs(y), 0.0);
  float curved = halfSize.x - corner + sqrt(max(0.0, corner * corner - delta * delta));
  vec2 integral = 0.5 + 0.5 * erf((x + vec2(-curved, curved)) * (sqrt(0.5) / sigma));
  return integral.y - integral.x;
}

// Return the mask for the shadow of a box from lower to upper
float roundedBoxShadow(vec2 lower, vec2 upper, vec2 point, float sigma, float corner) {
  // Center everything to make the math easier
  vec2 center = (lower + upper) * 0.5;
  vec2 halfSize = (upper - lower) * 0.5;

  // The signal is only non-zero in a limited range, so don't waste samples
  float low = point.y - halfSize.y;
  float high = point.y + halfSize.y;
  float start = clamp(-3.0 * sigma, low, high);
  float end = clamp(3.0 * sigma, low, high);

  // Accumulate samples (we can get away with surprisingly few samples)
  float step = (end - start) / 4.0;
  float y = start + step * 0.5;
  float value = 0.0;
  for (int i = 0; i < 4; i++) {
    value += roundedBoxShadowX(point.x, point.y - y, sigma, corner, halfSize) * gaussian(y, sigma) * step;
    y += step;
  }

  return value;
}

bool is_inside_ellipse(vec2 testPoint, vec2 ellipseCenter, vec2 ellipseSize) {
    float x = testPoint.x;
    float y = testPoint.y;
    float h = ellipseCenter.x;
    float k = ellipseCenter.y;

    float ellipseWidth = pow(ellipseSize.x, 2.0);
    float ellipseHeight = pow(ellipseSize.y, 2.0);

    float result = pow((x - h), 2.0f) / ellipseWidth + pow((y - k),2.0f) / ellipseHeight;
    
    return result <= 1;
}

void main() {

    float rectHeight = rectInPixels.w;
    float rectHalfHeight = rectHeight / 2.0f;
    
    float rectWidth = rectInPixels.z;
    float rectHalfWidth = rectWidth / 2.0f;
    
    vec2 rectHalfSize = vec2(rectHalfWidth, rectHalfHeight);
    
    vec2 fragCoord = uvs.xy * rectInPixels.zw - rectHalfSize;
    fragCoord = abs(fragCoord);

    float radius = uvs.x > 0.5f ? uvs.y > 0.5f ? borderRadius.y : borderRadius.z : uvs.y > 0.5f ? borderRadius.x : borderRadius.w;
    //float radius = uvs.x > 0.5f ? borderRadius.y : borderRadius.x;
    float borderWidth = uvs.x > 0.5f ? borderSize.y : borderSize.w;
    float borderHeight = uvs.y > 0.5 ? borderSize.x : borderSize.z;

    //float t = roundedBoxShadow(rectInPixels.xy + vec2(5, 5), rectInPixels.xy + rectInPixels.zw - vec2(10, 10), fragCoord, 3, 5);
    //f_Color = vec4(0, 0, 0, t);
    //return;

    vec2 pivotPosition = vec2(rectHalfWidth - radius, rectHalfHeight - radius);
    
    if (fragCoord.x > pivotPosition.x && fragCoord.y > pivotPosition.y) {
        float distance = length(fragCoord - pivotPosition);
        if (distance > radius) {
            discard;
        }

        if (borderHeight < radius && borderWidth < radius) {
            bool isInsideEllipse = is_inside_ellipse(fragCoord.xy, pivotPosition, vec2((radius - borderWidth), (radius - borderHeight)));
            if (isInsideEllipse) {
                f_Color = color;
                return;
            }
        }
    }
    else {
        if (fragCoord.x < rectHalfWidth - borderWidth && fragCoord.y < rectHalfHeight - borderHeight){
            f_Color = color;
            return;
        }
    }

 
    f_Color = borderColor;
}