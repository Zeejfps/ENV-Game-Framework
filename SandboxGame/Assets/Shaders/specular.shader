#BEGIN vertex_shader
#version 460 core

layout (location = 0) in vec3 attr_vertex_position;
layout (location = 1) in vec3 attr_vertex_normal;
layout (location = 2) in vec2 attr_vertex_uv;
layout (location = 3) in vec3 attr_vertex_tangent;

uniform mat4 matrix_projection, matrix_view;
uniform vec3 camera_position;

out vec3 normal;
out vec3 vert_position;
out vec3 FragPos;

flat out int instance_id;


//out vec3 frag_pos;
//out vec2 tex_coord;
//out vec3 tangent;

layout(std430, binding = 0) buffer model_matrices_t
{
    mat4 model_matrices[];
};

out VS_OUT
{
    vec3 frag_pos;
    vec2 tex_coord;
    vec3 tangent_view_position;
    vec3 tangent_frag_position;
    mat3 tangent_position;
} vs_out;

void main()
{
    mat4 matrix_model = model_matrices[gl_InstanceID];
    mat4 normal_matrix = transpose(inverse(matrix_model));

    vec4 vert_world_position = matrix_model * vec4(attr_vertex_position, 1);
    vec4 vert_view_position = matrix_view * vert_world_position;

    FragPos = vert_world_position.xyz;

    gl_Position = matrix_projection * vert_view_position;
    vert_position = vec3(vert_world_position) / vert_world_position.w;

    vs_out.frag_pos = vec3(matrix_model * vec4(attr_vertex_position,1.0));
    normal = (normal_matrix * vec4(attr_vertex_normal, 0)).xyz;

    vec3 T = normalize(normal_matrix * vec4(attr_vertex_tangent, 0)).xyz;
    vec3 N = normalize(normal);
    T = normalize(T - dot(T,N) * N);
    vec3 B = cross(N,T);

    mat3 TBN = transpose(mat3(T,B,N));

    vs_out.tex_coord = attr_vertex_uv;
    vs_out.tangent_position = TBN;
    vs_out.tangent_view_position = TBN * camera_position;
    vs_out.tangent_frag_position = TBN * vs_out.frag_pos;
    instance_id = gl_InstanceID;
}
#END

#BEGIN fragment_shader

#version 460 core
layout (location = 0) out vec4 out_result;
layout (location = 1) out vec3 out_normal;
layout (location = 2) out vec3 out_world;
layout (location = 3) out vec3 out_depth;

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

flat in int instance_id;

in VS_OUT {
    vec3 frag_pos;
    vec2 tex_coord;
    vec3 tangent_view_position;
    vec3 tangent_frag_position;
//TBN MATRIX
    mat3 tangent_position;
} fs_in;

in vec3 FragPos;
in vec3 normal;
in vec3 vert_position;
uniform vec3 camera_position;
layout (location = 0) uniform Material material;
uniform Light light;

layout(std430, binding = 0) buffer model_matrices_t
{
    mat4 model_matrices[];
};

void main()
{
    //float distance = distance(light.position,vert_position);
    //float distance = (lightDir);

    //occlusion
    //vec3 occlusion = texture(material.occlusion,fs_in.tex_coord).rgb;
    vec3 color = texture(material.diffuse, fs_in.tex_coord).rgb;
    //ambient
    //vec3 ambient = light.ambient * color * occlusion;
    
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
    //vec3 light_direction = normalize((fs_in.tangent_position * light.position) - fs_in.tangent_frag_position);
    //float diff = max(dot(light_direction, normal_map),0.0);
    //vec3 diffuse = light.diffuse * diff * color;
    
    //specular
    //vec3 view_direction = normalize(fs_in.tangent_view_position - fs_in.tangent_frag_position);
    //vec3 reflect_direction = reflect(-light_direction, normal_map);
    //float spec = pow(max(dot(view_direction, reflect_direction), 0.0), material.shininess);
    //vec3 specular = light.specular * spec * roughness.r;
    
    //emission
    //vec3 emission = texture(material.emission, fs_in.tex_coord).rgb;
    
    //vec3 result = ambient + diffuse + specular;// + translucency_final;// + emission;
    
    mat4 matrix_model = model_matrices[instance_id];
    
    float depth = gl_FragCoord.z / gl_FragCoord.w;
    
    depth *= 0.01;
    
    out_result = vec4(color, depth);
    vec3 normal_o = normal_map * fs_in.tangent_position; 
    out_normal = normalize(mat3(matrix_model) * normal_o);
    out_world = FragPos;
}
#END