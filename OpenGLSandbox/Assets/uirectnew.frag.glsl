in vec4 uvs;
in vec4 color;
in vec4 borderSize;
in vec4 borderColor;
in vec4 borderRadius;
in vec4 rectInPixels;

out vec4 f_Color;

//vec2 bezier(vec2 P0, vec2 P1, vec2 P01, vec2 P12, vec2 P3, float t) {
//    float u = 1.0 - t;
//    float tt = t * t;
//    float uu = u * u;
//    float uuu = uu * u;
//    float ttt = tt * t;
//
//    vec2 B = (uuu * P0) + (3.0 * uu * t * P01) + (3.0 * u * tt * P12) + (ttt * P3);
//
//    return B;
//}

vec2 bezier(vec2 p0, vec2 p1, vec2 c0, vec2 c1, float t) {
    float tInv = 1.0 - t;
    float tInv2 = tInv * tInv;
    float t2 = t * t;

    // Calculate blending functions
    float B0 = tInv2 * tInv;
    float B1 = 3.0 * tInv2 * t;
    float B2 = 3.0 * tInv * t2;
    float B3 = t2 * t;

    // Calculate the x and y coordinates of the point on the curve
    vec2 result = B0 * p0 + B1 * c0 + B2 * c1 + B3 * p1;

    return result;
}

void main() {

    float rectHeight = rectInPixels.w;
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

        float distToTop = outerRadius - borderHeight;
        float distToLeft = outerRadius - borderWidth;
        vec2 innerTop = pivotPosition + vec2(0.0f, distToTop);
        vec2 innerLeft = pivotPosition - vec2(distToLeft, 0.0f);
        
        vec2 P0 = innerLeft;  // Starting anchor point
        vec2 P3 = innerTop; // Ending anchor point
        vec2 P1 = pivotPosition + vec2(-distToLeft, distToTop);  // Point towards which handles point
        vec2 P01 = P1;  // Control point for P0
        vec2 P12 = P1;  // Control point for P3
        
        float t;
        if (distToTop > distToLeft) {
            t = (fragCoord.y - pivotPosition.y) / (innerTop.y - pivotPosition.y);
        }
        else {
            t = (fragCoord.x - innerLeft.x) / (pivotPosition.x - innerLeft.x);
        }
        
        vec2 pointOnCurve = bezier(P0, P3, P01, P12, t);
        
        if (fragCoord.x > pointOnCurve.x && fragCoord.y < pointOnCurve.y) {
            discard;
        }
        
        float distToCurve = length(fragCoord - pointOnCurve);
        //color = vec4(0.0f, distToCurve / 20.0f, 0.0f, 1.0f);
        
//        float distanceToInnerTop = (length(fragCoord - innerTop));
//        float distanceToInnerLeft = (length(fragCoord - innerLeft));
//
//        innerRadius = mix(distToLeft, distToTop, (fragCoord.x - innerLeft.x) / (pivotPosition.x - innerLeft.x));
//        color = vec4(0.0f, innerRadius / 20.0f, 0.0f, 1.0f);
//
//        if (distToCurve < 1.0f)
//            discard;
    }
    else {
        if (fragCoord.x > borderWidth && fragCoord.y < rectHeight - borderHeight)
            discard;
    }

    f_Color = color;
}