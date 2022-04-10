#version 460
uniform vec3 color = vec3(1,1,1);

out vec4 out_result;

void main() {
    out_result = vec4(vec3(color), 1.0);
}
