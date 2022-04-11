#version 460 core
out vec4 out_result;

struct Material {
    
    sampler2D specular;
    sampler2D emission;
    float shininess;
};

struct Light {
    vec3 position;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

in VS_OUT {
    vec3 frag_pos;
    vec2 tex_coord;
    vec3 tangent_view_position;
    vec3 tangent_frag_position;
    mat3 tangent_position;
} fs_in;

in vec3 normal;
in vec3 vert_position;
//in vec2 tex_coord;
//in vec3 frag_pos;
//in vec3 tangent;

layout (location = 0) uniform sampler2D m_diffuse;
layout (location = 1) uniform sampler2D m_normal_map;
uniform vec3 camera_position;
uniform Material material;
uniform Light light;
//uniform sampler2D texture_main;

void main()
{
    
    //ambient
    vec3 ambient = light.ambient * texture(m_diffuse, fs_in.tex_coord).rgb;
    
    //normal
    vec3 normal_map = texture(m_normal_map, fs_in.tex_coord).rgb;
    normal_map = normalize(normal * 2.0 - 1.0);
    
    //diffuse
    vec3 norm = normalize(normal);
    vec3 light_direction = normalize((light.position * fs_in.tangent_position) - fs_in.tangent_frag_position);
    float diff = max(dot(norm, light_direction),0.0);
    vec3 diffuse = light.diffuse * diff * texture(m_diffuse, fs_in.tex_coord).rgb;
    
    //specular
    vec3 view_direction = normalize(fs_in.tangent_view_position - fs_in.tangent_frag_position);
    vec3 reflect_direction = reflect(-light_direction, norm);
    float spec = pow(max(dot(view_direction, reflect_direction), 0.0), material.shininess);
    vec3 specular = light.specular * spec;// * texture(material.specular, tex_coord).rgb;
    
    //emission
    vec3 emission = texture(material.emission, fs_in.tex_coord).rgb;
    
    vec3 result = ambient + diffuse + specular;// + emission;
    
    out_result = vec4(result, 1.0);
}
