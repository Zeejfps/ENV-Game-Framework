#BEGIN vertex_shader

#version 460 core

const int MAX_BATCH_SIZE = 128;

layout (location = 0) in vec3 attr_vertex_position;
layout (location = 2) in vec3 attr_vertex_uvs;

uniform sampler2D sprite_sheet;
uniform vec2 texture_size;
uniform mat4 matrix_projection, matrix_view;
uniform mat4 model_matrices[MAX_BATCH_SIZE];
uniform vec2 offsets[MAX_BATCH_SIZE];
uniform vec2 sizes[MAX_BATCH_SIZE];

out vec2 frag_uvs;
flat out int instanceID;

void main()
{
    instanceID = gl_InstanceID;
    mat4 matrix_model = model_matrices[instanceID];
    vec4 vert_world_position = matrix_model * vec4(attr_vertex_position, 1);
    vec4 vert_view_position = matrix_view * vert_world_position;
    gl_Position = matrix_projection * vert_view_position;
 
    vec2 offset = offsets[instanceID];
    vec2 size = sizes[instanceID];
    
    float x = (offset.x / texture_size.x) + (size.x / texture_size.x) * attr_vertex_uvs.x;
    float y = (offset.y / texture_size.y) + (size.y / texture_size.y) * attr_vertex_uvs.y;
    frag_uvs = vec2(x, y);
}
    
#END
    
#BEGIN fragment_shader
    
#version 460 core

const int MAX_BATCH_SIZE = 128;

struct SpriteData {
    vec3 Color;
    mat4 ModelMatrix;
};

uniform sampler2D sprite_sheet;
uniform vec3 colors[MAX_BATCH_SIZE];

flat in int instanceID;
in vec2 frag_uvs;

out vec4 out_result;

void main() {
    const vec3 color = colors[instanceID];
    const vec4 texture_color = texture(sprite_sheet, frag_uvs);
    out_result = texture_color;
}

#END
