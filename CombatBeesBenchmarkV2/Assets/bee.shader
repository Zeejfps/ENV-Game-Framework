#BEGIN vertex_shader

#version 460 core

const int MAX_BATCH_SIZE = 100000;

struct Bee {
  vec4 color;
  mat4 modelMatrix;
};

layout (location = 0) in vec4 attr_vertex_position;

uniform mat4 matrix_projection, matrix_view;
layout(std140) uniform beeDataBlock {
    Bee bees[MAX_BATCH_SIZE];    
};

flat out int instanceID;

void main()
{
    instanceID = gl_InstanceID;
    mat4 matrix_model = bees[instanceID].modelMatrix;
    vec4 vert_world_position = matrix_model * attr_vertex_position;
    vec4 vert_view_position = matrix_view * vert_world_position;
    gl_Position = matrix_projection * vert_view_position;
}
    
#END
    
#BEGIN fragment_shader
    
#version 460 core

const int MAX_BATCH_SIZE = 100000;

struct Bee {
  vec4 color;
  mat4 modelMatrix;
};

layout(std140) uniform beeDataBlock {
    Bee bees[MAX_BATCH_SIZE];    
};

flat in int instanceID;

out vec4 out_result;

void main() {
    const vec4 color = bees[instanceID].color;
    out_result = color;
}

#END
