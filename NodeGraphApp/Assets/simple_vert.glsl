#version 330

layout(location=0) in vec2 in_position;

uniform vec4 u_rect;
uniform mat4 u_vp;

void main() {
    vec4 position = vec4(in_position.x, in_position.y, 0, 1);
    gl_Position = u_vp * position;
}
