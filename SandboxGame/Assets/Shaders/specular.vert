#version 460 core

layout (location = 0) in vec3 attr_vertex_position;
layout (location = 1) in vec3 attr_vertex_normal;
layout (location = 2) in vec2 attr_vertex_uv;
layout (location = 3) in vec3 attr_vertex_tangent;

uniform mat4 matrix_projection, matrix_view, matrix_model;
uniform vec3 camera_position;

out vec3 normal;
out vec3 vert_position;
out vec3 FragPos;
//out vec3 frag_pos;
//out vec2 tex_coord;
//out vec3 tangent;

out VS_OUT
{
    vec3 frag_pos;
    vec2 tex_coord;
    vec3 tangent_view_position;
    vec3 tangent_frag_position;
    mat3 tangent_position;
} vs_out;

void main()
{
    mat4 normal_matrix = transpose(inverse(matrix_model));
    
    vec4 vert_world_position = matrix_model * vec4(attr_vertex_position, 1);
    vec4 vert_view_position = matrix_view * vert_world_position;

    FragPos = vert_world_position.xyz;
    
    gl_Position = matrix_projection * vert_view_position;
    vert_position = vec3(vert_world_position) / vert_world_position.w;

    vs_out.frag_pos = vec3(matrix_model * vec4(attr_vertex_position,1.0));
    normal = (normal_matrix * vec4(attr_vertex_normal, 0)).xyz;
    
    vec3 T = normalize(normal_matrix * vec4(attr_vertex_tangent, 0)).xyz;
    vec3 N = normalize(normal);
    T = normalize(T - dot(T,N) * N);
    vec3 B = cross(N,T);
    
    mat3 TBN = transpose(mat3(T,B,N));
    
    vs_out.tex_coord = attr_vertex_uv;
    vs_out.tangent_position = TBN;
    vs_out.tangent_view_position = TBN * camera_position;
    vs_out.tangent_frag_position = TBN * vs_out.frag_pos; 
}