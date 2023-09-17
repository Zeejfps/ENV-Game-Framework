#version 430

in vec4 borderRadius;
in vec4 uvs;
in vec4 color;

out vec4 f_Color;

float roundedBoxSDF(vec2 CenterPosition, vec2 Size, float Radius) {
    return length(max(abs(CenterPosition)-Size+Radius,0.0))-Radius;
}

void main() {
    vec2 rectSize = vec2(150.0f, 150.0f);
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
    
    float alpha = 1.0f - clamp(roundedBoxSDF(fragCoord - rectHalfSize, rectHalfSize, radius), 0, 1);
    
    f_Color = vec4(color.rgb * alpha, alpha);
}