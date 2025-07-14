#version 410

in vec2 texCoords;

uniform sampler2D tex;

out vec4 f_Color;

void main() {
    f_Color = texture(tex, texCoords);
}