#version 460 core
layout (location = 0) out vec4 out_result;
layout (location = 1) out vec3 out_normal;
layout (location = 2) out vec3 out_world;

struct Material {
    sampler2D diffuse;
    sampler2D normal_map;
    sampler2D roughness;
    sampler2D occlusion;
    sampler2D translucency;
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

in vec3 FragPos;
in vec3 normal;
in vec3 vert_position;

uniform vec3 camera_position;
layout (location = 0) uniform Material material;
uniform Light light;

void main()
{
    float distance = distance(light.position,vert_position);
    //float distance = (lightDir);

    //occlusion
    vec3 occlusion = texture(material.occlusion,fs_in.tex_coord).rgb;
    vec3 color = texture(material.diffuse, fs_in.tex_coord).rgb;
    //ambient
    vec3 ambient = light.ambient * color * occlusion;
    
    //normal
    vec3 normal_map = texture(material.normal_map, fs_in.tex_coord).rgb;
    normal_map = normalize(normal_map * 2.0 - 1.0);
    
    //roughness
    vec3 roughness = texture(material.roughness, fs_in.tex_coord).rgb;
    
    //translucency
    vec3 translucency = texture(material.translucency, fs_in.tex_coord).rgb;
    vec3 translucency_color = color * vec3(.3,.4,0) * 0.8f;
    vec3 translucency_final = mix(vec3(0,0,0),translucency_color,translucency.r);
    
    //diffuse
    vec3 light_direction = normalize((fs_in.tangent_position * light.position) - fs_in.tangent_frag_position);
    float diff = max(dot(light_direction, normal_map),0.0);
    vec3 diffuse = light.diffuse * diff * color;
    
    //specular
    vec3 view_direction = normalize(fs_in.tangent_view_position - fs_in.tangent_frag_position);
    vec3 reflect_direction = reflect(-light_direction, normal_map);
    float spec = pow(max(dot(view_direction, reflect_direction), 0.0), material.shininess);
    vec3 specular = light.specular * spec * roughness.r;
    
    //emission
    //vec3 emission = texture(material.emission, fs_in.tex_coord).rgb;
    
    vec3 result = ambient + diffuse + specular;// + translucency_final;// + emission;
    
    out_result = vec4(result, 1.0);
    out_normal = normalize(normal);
    out_world = FragPos;
}
