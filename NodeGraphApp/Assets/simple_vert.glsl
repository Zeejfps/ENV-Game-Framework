#version 330

layout(location=0) in vec2 in_position;

uniform vec4 u_rect;
uniform mat4 u_vp;

void main() {
    gl_Position = vec4(in_position.x, in_position.y, 0, 1);
}
