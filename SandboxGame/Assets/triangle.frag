#version 330 core

in vec3 vertex_normal;
in vec3 lightColor;

out vec4 out_result;

uniform vec3 color;

void main()
{
    
    out_result = vec4(lightColor, 1.0);
} 