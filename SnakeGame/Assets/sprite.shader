#BEGIN vertex_shader

#version 460 core

struct SpriteData {
    vec3 Color;
    mat4 ModelMatrix;
};

layout (location = 0) in vec3 attr_vertex_position;
uniform mat4 matrix_projection, matrix_view, matrix_model;
uniform SpriteData batch[256];

flat out int instanceID;

void main()
{
    instanceID = gl_InstanceID;
    vec4 vert_world_position = matrix_model * vec4(attr_vertex_position, 1);
    vec4 vert_view_position = matrix_view * vert_world_position;
    gl_Position = matrix_projection * vert_view_position;
}
    
#END
    
#BEGIN fragment_shader
    
#version 460 core

struct SpriteData {
    vec3 Color;
    mat4 ModelMatrix;
};

uniform vec3 color = vec3(1,1,1);

flat in int instanceID;

out vec4 out_result;

void main() {
    //const float t = model_matrices[instanceID];
    out_result = vec4(color, 1);
}

#END
