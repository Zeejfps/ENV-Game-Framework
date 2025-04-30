#version 330 core
layout(points) in;
layout(triangle_strip, max_vertices = 124) out; // MAX_POINTS = number of curve samples

uniform float thickness;
uniform vec2 u_p0, u_p1, u_p2, u_p3;
uniform mat4 projection;

void main() {

    vec2 points[32];
    for (int i = 0; i < 32; ++i) {
           
        float t = float(i) / 31.0;
        float u = 1.0 - t;
        vec2 pos =
                u*u*u * u_p0 +
                3*u*u*t * u_p1 +
                3*u*t*t * u_p2 +
                t*t*t * u_p3;
        points[i] = pos;
    }

    for (int i = 0; i < 32; ++i) {
        vec2 pos = points[i];

        vec2 tangent;
        if (i == 0) {
            tangent = normalize(points[i + 1] - pos);
        } else if (i == 32 - 1) {
            tangent = normalize(pos - points[i - 1]);
        } else {
            tangent = normalize(points[i + 1] - points[i - 1]);
        }

        // Perpendicular direction (normal)
        vec2 normal = vec2(-tangent.y, tangent.x);
        vec2 offset = normal * (thickness * 0.5);

        // Emit top and bottom edge of the strip
        gl_Position = projection * vec4(pos + offset, 0.0, 1.0);
        EmitVertex();
        
        gl_Position = projection * vec4(pos - offset, 0.0, 1.0);
        EmitVertex();
    }

    EndPrimitive();
}