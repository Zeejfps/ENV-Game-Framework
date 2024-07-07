#version 460

in vec4 f_Tint;
in vec2 f_uvCoords;

uniform sampler2D tex;

out vec4 f_Color;

void main() {
    vec4 sample_color = texture(tex, f_uvCoords.rg); 
    vec4 tintedColor = sample_color * f_Tint;
    f_Color = tintedColor;
}