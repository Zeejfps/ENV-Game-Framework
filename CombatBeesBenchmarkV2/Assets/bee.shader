#BEGIN vertex_shader

#version 460 core

const int MAX_BATCH_SIZE = 50000;

layout (location = 0) in vec3 attr_vertex_position;
uniform mat4 matrix_projection, matrix_view;
uniform modelMatricesBlock {
    mat4 modelMatrices[MAX_BATCH_SIZE];    
};
flat out int instanceID;

void main()
{
    instanceID = gl_InstanceID;
    mat4 matrix_model = modelMatrices[instanceID];
    vec4 vert_world_position = matrix_model * vec4(attr_vertex_position, 1);
    vec4 vert_view_position = matrix_view * vert_world_position;
    gl_Position = matrix_projection * vert_view_position;
}
    
#END
    
#BEGIN fragment_shader
    
#version 460 core

const int MAX_BATCH_SIZE = 50000;

uniform colorsBlock {
    vec4 colors[MAX_BATCH_SIZE];
};

flat in int instanceID;

out vec4 out_result;

void main() {
    const vec4 color = colors[instanceID];
    out_result = color;
}

#END
