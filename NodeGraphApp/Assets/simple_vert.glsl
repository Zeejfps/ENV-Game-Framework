#version 330

layout(location=0) in vec2 in_position;

uniform vec4 u_rect;
uniform mat4 u_vp;

void main() {
    vec2 scaledPosition = u_rect.xy + in_position * u_rect.zw;
    vec4 position = vec4(scaledPosition.x, scaledPosition.y, 0, 1);
    gl_Position = u_vp * position;
}
