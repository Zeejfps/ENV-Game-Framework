#version 460 core

in vec2 UVs;

out vec4 FragColor;

void main() 
{
    FragColor = vec4(UVs.x, UVs.y, 0.0, 1.0);
}