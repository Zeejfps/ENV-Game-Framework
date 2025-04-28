#version 330

layout(location = 0) in vec2 attr_Position;

uniform vec4 u_rect;
uniform mat4 u_viewProjMat;

void main() {

    vec2 scaledPosition = u_rect.xy + attr_Position * u_rect.zw;
    vec4 position = vec4(scaledPosition.x, scaledPosition.y, 0, 1);
    gl_Position = u_viewProjMat * position;
}
