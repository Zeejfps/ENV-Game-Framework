#version 460 core

layout (location = 0) in vec3 attr_vertex_position;
layout (location = 1) in vec3 attr_vertex_normal;

out vec3 vertex_normal;
out vec3 lightColor;

uniform mat4 matrix_projection;
uniform mat4 matrix_view;
uniform mat4 normal_matrix;

void main()
{
    vertex_normal = attr_vertex_normal;
    
    vec3 LightPosition = vec3(2,5,5);
    vec4 N = normalize(normal_matrix*vec4(attr_vertex_normal,1));
    
    vec3 color = vec3(.9,.9,.8);
    vec3 ambient_color = vec3(.3,.3,.5);
    
    vec3 L = normalize(LightPosition - attr_vertex_position.xyz);
    float lam = max(dot(N.xyz,L), 0.0);
    
    vec4 model_Pos = vec4(attr_vertex_position.x, attr_vertex_position.y, attr_vertex_position.z, 1.0);
    vec4 viewPos = matrix_view * model_Pos;
    
    lightColor = clamp(lam * color + ambient_color, 0.0, 1.0);
    
    gl_Position = matrix_projection * viewPos;
}