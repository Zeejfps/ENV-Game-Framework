in vec4 uvs;
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
    float rectHalfHeight = rectHeight * 2.0f;
    
    float rectWidth = rectInPixels.z;
    float rectHalfWidth = rectWidth * 2.0f;
    
    vec2 fragCoord = uvs.xy * rectInPixels.zw;

    float outerRadius = borderRadius.x;
    float borderWidth = borderSize.w;
    float borderHeight = borderSize.x;

    vec4 color = vec4(1.0, 0.0, 1.0, 1.0);
    vec2 pivotPosition = vec2(outerRadius, rectHeight - outerRadius);

    if (fragCoord.x < pivotPosition.x && fragCoord.y > pivotPosition.y) {
        float distance = length(fragCoord - pivotPosition);

        if (distance > outerRadius) {
            discard;
        }
        
        bool isInsideEllipse = is_inside_ellipse(fragCoord.xy, pivotPosition, vec2((outerRadius - borderWidth), (outerRadius - borderHeight)));
        if (isInsideEllipse) {
            discard;
        }
    }
    else {
        if (fragCoord.x > borderWidth && fragCoord.y < rectHeight - borderHeight)
            discard;
    }
    
    f_Color = color;
}