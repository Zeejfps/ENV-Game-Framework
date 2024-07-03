#version 460

in vec2 f_uvCoords;

uniform sampler2D tex;

out vec4 f_Color;

void main() {
    //* vec2(0.1171875, 0.0390625 + 0.078125)
    f_Color = texture(tex, f_uvCoords.rg);
}