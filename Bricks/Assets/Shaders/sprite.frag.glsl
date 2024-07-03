#version 460

in vec4 f_uvCoords;

out vec4 f_Color;

void main() {
    f_Color = vec4(f_uvCoords.rgb, 1);
}