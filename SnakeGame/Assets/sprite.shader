#BEGIN vertex_shader

#version 460 core

const int MAX_BATCH_SIZE = 512;

layout (location = 0) in vec3 attr_vertex_position;
uniform mat4 matrix_projection, matrix_view;
uniform mat4 model_matrices[MAX_BATCH_SIZE];

flat out int instanceID;

void main()
{
    instanceID = gl_InstanceID;
    mat4 matrix_model = model_matrices[instanceID];
    vec4 vert_world_position = matrix_model * vec4(attr_vertex_position, 1);
    vec4 vert_view_position = matrix_view * vert_world_position;
    gl_Position = matrix_projection * vert_view_position;
}
    
#END
    
#BEGIN fragment_shader
    
#version 460 core

const int MAX_BATCH_SIZE = 512;

struct SpriteData {
    vec3 Color;
    mat4 ModelMatrix;
};

uniform vec3 colors[MAX_BATCH_SIZE];

flat in int instanceID;

out vec4 out_result;

void main() {
    const vec3 color = colors[instanceID];
    out_result = vec4(color, 1);
}

#END
