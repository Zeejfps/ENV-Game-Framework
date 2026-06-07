#version 400

// Input from the vertex shader. The GPU automatically interpolates
// the v_color values from the triangle's three vertices.
in vec3 v_color;

// The final output color of the fragment (pixel)
out vec4 FragColor;

void main() {
    // Just apply the interpolated color.
    FragColor = vec4(v_color, 1.0);
}