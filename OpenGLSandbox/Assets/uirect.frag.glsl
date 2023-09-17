#version 430

in vec4 borderRadius;
in vec4 uvs;
in vec4 color;
in vec4 rectInPixels;

out vec4 f_Color;

vec4 color_blend(vec4 dst, vec4 src) {
    return dst * (1.0 - src.a) + src * src.a;
}

float sdRoundBox( in vec2 p, in vec2 b, in vec4 r ){
    r.xy = (p.x>0.0)?r.xy : r.zw;
    r.x  = (p.y>0.0)?r.x  : r.y;
    vec2 q = abs(p)-b+r.x;
    return min(max(q.x,q.y),0.0) + length(max(q,0.0)) - r.x;
}

float roundedBoxSDF(vec2 CenterPosition, vec2 Size, float Radius) {
    return length(max(abs(CenterPosition)-Size+Radius,0.0))-Radius;
}

float sdf_border(in float d, in float thickness) {
    return d <= 0.0 ? 1.0 - smoothstep(thickness - 0.4, thickness + 0.4, abs(d)) : 0.0;
}

float sdf_fill(float d, float softness) {
    d = 1.0 - d;
    return smoothstep(1.0 - softness, 1.0 + softness, d);
}

void main() {
    vec2 rectSize = vec2(rectInPixels.z, rectInPixels.w);
    vec2 rectHalfSize = rectSize / 2.0f;
    vec2 fragCoord = uvs.xy * rectSize;
    float radius;
    if (uvs.x > 0.5) {
        if (uvs.y > 0.5) {
            // Top Right
            radius = borderRadius.y;
        }
        else {
            // Bottom Right
            radius = borderRadius.z;
        }
    }
    else  {  
        if (uvs.y > 0.5) {
            // Top Left
            radius = borderRadius.x;
        }
        else {
            // Bottom Left
            radius = borderRadius.w;
        }
    }
    
    //float distance = roundedBoxSDF(fragCoord - rectHalfSize, rectHalfSize, radius);
    //f_Color = vec4(distance, 0, 0, 1);
    
    float distance = sdRoundBox(fragCoord - rectHalfSize, rectHalfSize, vec4(radius));
    float border = sdf_border(distance, 10.0f);
    float fill = sdf_fill(distance, 0.5f);

    vec4 fillColor   = color; // red
    vec4 borderColor = vec4(0.0, 1.0, 0.0, 1.0); // green
    
    f_Color = color_blend(f_Color, vec4(fillColor   * fill));
    f_Color = color_blend(f_Color, vec4(borderColor * border));
    
    //float alpha = 1.0f - clamp(distance, 0, 1);
    //f_Color = vec4(color.rgb * alpha, alpha);
}