#version 330

layout(location = 0) in float t;

uniform vec2 u_p0, u_p1, u_p2, u_p3;

out vec2 worldPos;

void main() {
    float u = 1.0 - t;
    vec2 pos =
        u*u*u * u_p0 +
        3*u*u*t * u_p1 +
        3*u*t*t * u_p2 +
        t*t*t * u_p3;

    worldPos = pos;
}