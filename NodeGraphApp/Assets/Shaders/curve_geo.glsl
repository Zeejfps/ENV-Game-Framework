#version 330 core
layout(lines) in;
layout(triangle_strip, max_vertices = 4) out;

uniform float thickness;
uniform mat4 projection;

in vec2 worldPos[2];
out vec2 fragUV; // optional if you want gradient, etc.

void main() {
    vec2 p0 = worldPos[0];
    vec2 p1 = worldPos[1];

    vec2 dir = normalize(p1 - p0);
    vec2 normal = vec2(-dir.y, dir.x);
    vec2 offset = normal * (thickness * 0.5);

    // Emit 2 triangles = quad
    gl_Position = projection * vec4(p0 + offset, 0.0, 1.0);
    EmitVertex();
    gl_Position = projection * vec4(p0 - offset, 0.0, 1.0);
    EmitVertex();
    gl_Position = projection * vec4(p1 + offset, 0.0, 1.0);
    EmitVertex();
    gl_Position = projection * vec4(p1 - offset, 0.0, 1.0);
    EmitVertex();
    EndPrimitive();
}