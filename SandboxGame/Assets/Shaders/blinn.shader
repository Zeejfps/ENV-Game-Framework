#BEGIN vertex_shader
#version 460 core

layout (location = 0) in vec3 attr_vertex_position;
layout (location = 1) in vec3 attr_vertex_normal;
layout (location = 2) in vec2 attr_vertex_uv;

uniform mat4 matrix_projection, matrix_view, matrix_model, normal_matrix;

out vec3 normal_interp;
out vec3 vert_position;
out vec2 tex_coord;

void main()
{
    vec4 vert_world_position = matrix_model * vec4(attr_vertex_position, 1);
    vec4 vert_view_position = matrix_view * vert_world_position;

    gl_Position = matrix_projection * vert_view_position;
    vert_position = vec3(vert_world_position) / vert_world_position.w;
    normal_interp = vec3(normal_matrix * vec4(attr_vertex_normal,0.0));

    tex_coord = attr_vertex_uv;
}
    
#END

#BEGIN fragment_shader
#version 460 core

uniform vec3 camera_position;
uniform vec3 light_position;

in vec3 normal_interp;
in vec3 vert_position;
in vec2 tex_coord;


//const vec3 light_position = vec3(1,3,-2);
const vec3 light_color = vec3(1,1,1);
const float shininess = 15.0;
const float light_power = 40.0;
const float screen_gamma = 2.2;

const vec3 ambient_color = vec3(.05,.05,.1);
const vec3 color = vec3(1.0,1.0,1.0);
const vec3 spec_color = vec3(.2,.2,.2);
const vec3 diffuse_color = vec3(1.0,0.0,0.0);

uniform sampler2D texture_main;

out vec4 out_result;

void main()
{
    vec3 normal = normalize(normal_interp);
    vec3 light_direction = light_position - vert_position;
    float distance = length(light_direction) * 0.5f;
    
    //distance *= distance;
    float intensity = 1.0 / (3 * distance * distance + 0.7 * distance + 1);
    
    light_direction = max(normalize(light_direction),0);
    
    float lambertian = max(dot(light_direction, normal),0);
    float specular = 0.0;
    
    if (lambertian != 0.0)
    {
        vec3 view_direction = normalize(camera_position - vert_position);
//      vec3 reflect_direction = reflect(-light_direction, normal);
//      float specular_angle = max(dot(reflect_direction,view_direction),0.0);
        vec3 half_direction = normalize(light_direction + view_direction);
        float specular_angle = max(dot(half_direction,normal),0.0);
        specular = pow(specular_angle, shininess) / 8;
    }
    
    vec3 color_linear = clamp(ambient_color + texture(texture_main, tex_coord).rgb * (lambertian + mix(ambient_color,vec3(0,0,0),lambertian)) * light_color / distance + spec_color * specular * light_color * light_power / distance,0.0,1.0);
    //vec3 color_linear = ((lambertian * intensity + ambient_color) + specular * intensity) * light_color;
    //vec3 color_gamma_corrected = pow(color_linear, vec3(1.0 / screen_gamma));
    
    //out_result = vec4(color_linear, 1.0);
    out_result = vec4(vec3(color_linear),1);
} 

#END