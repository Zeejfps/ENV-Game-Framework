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
    vec2 fragCoord = uvs.xy * rectSize;
    float radius;
    if (uvs.x > 0.5) { // On the right side
        if (uvs.y > 0.5) {
            // Top Side
            radius = borderRadius.y;
        }
        else {
            // Bottom side
            radius = borderRadius.z;
        }
    }
    else  {  // On the left side
        if (uvs.y > 0.5) {
            // Top Side
            radius = borderRadius.x;
        }
        else {
            radius = borderRadius.w;
        }
    }
    f_Color = vec4(1.0f - roundedBoxSDF(fragCoord - rectSize / 2.0f, rectSize / 2.0f, radius), 0.0f, 0.0f, 1.0f);
}