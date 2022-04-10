#version 460 core

layout (location = 0) in vec3 attr_vertex_position;
layout (location = 1) in vec3 attr_vertex_normal;
layout (location = 2) in vec2 attr_vertex_uv;

uniform mat4 matrix_projection, matrix_view, matrix_model, normal_matrix;

out vec3 normal;
out vec3 vert_position;
out vec3 frag_pos;
out vec2 tex_coord;

void main()
{
    vec4 vert_world_position = matrix_model * vec4(attr_vertex_position, 1);
    vec4 vert_view_position = matrix_view * vert_world_position;

    gl_Position = matrix_projection * vert_view_position;
    vert_position = vec3(vert_world_position) / vert_world_position.w;
    
    frag_pos = vec3(matrix_model * vec4(attr_vertex_position,1.0));
    normal = mat3(transpose(inverse(matrix_model))) * attr_vertex_normal;

    tex_coord = attr_vertex_uv;
}