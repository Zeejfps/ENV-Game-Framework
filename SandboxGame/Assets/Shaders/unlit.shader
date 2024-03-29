﻿#BEGIN vertex_shader

#version 460
layout (location = 0) in vec3 attr_vertex_position;
uniform mat4 matrix_projection, matrix_view, matrix_model;

void main()
{
    vec4 vert_world_position = matrix_model * vec4(attr_vertex_position, 1);
    vec4 vert_view_position = matrix_view * vert_world_position;
    gl_Position = matrix_projection * vert_view_position;
}
    
#END
    
#BEGIN fragment_shader    

#version 460
uniform vec3 color = vec3(1,1,1);

out vec4 out_result;

void main() {
    out_result = vec4(vec3(color), 1.0);
}

#END
