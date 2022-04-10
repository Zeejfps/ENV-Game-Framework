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

in vec3 normal;
in vec3 vert_position;
in vec2 tex_coord;
in vec3 frag_pos;
in vec3 tangent;

layout (location = 0) uniform sampler2D m_diffuse;
layout (location = 1) uniform sampler2D m_normal_map;
uniform vec3 camera_position;
uniform Material material;
uniform Light light;
//uniform sampler2D texture_main;

void main()
{
    
    //ambient
    vec3 ambient = light.ambient * texture(m_diffuse, tex_coord).rgb;
    
    //normal
    vec3 normal_map = texture(m_normal_map, tex_coord).rgb;
    //normal_map = normalize(normal * 2.0 - 1.0);
    
    //diffuse
    vec3 norm = normalize(normal);
    vec3 light_direction = normalize(light.position - frag_pos);
    float diff = max(dot(norm, light_direction),0.0);
    vec3 diffuse = light.diffuse * diff * texture(m_diffuse, tex_coord).rgb;
    
    //specular
    vec3 view_direction = normalize(camera_position - frag_pos);
    vec3 reflect_direction = reflect(-light_direction, norm);
    float spec = pow(max(dot(view_direction, reflect_direction), 0.0), material.shininess);
    vec3 specular = light.specular * spec;// * texture(material.specular, tex_coord).rgb;
    
    //emission
    vec3 emission = texture(material.emission, tex_coord).rgb;
    
    vec3 result = ambient + diffuse + specular;// + emission;
    
    out_result = vec4(tangent, 1.0);
}
