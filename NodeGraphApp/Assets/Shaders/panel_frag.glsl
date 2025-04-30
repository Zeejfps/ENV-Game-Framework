#version 330

in vec2 uvs;
in vec4 color;
in vec4 borderSize;
in vec4 borderColor;
in vec4 borderRadius;
in vec4 rectInPixels;

out vec4 f_Color;

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
    float borderWidth = uvs.x > 0.5f ? borderSize.y : borderSize.w;
    float borderHeight = uvs.y > 0.5 ? borderSize.x : borderSize.z;

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