#BEGIN vertex_shader

#version 330 core
layout (location = 0) in vec2 aPos;
layout (location = 2) in vec2 aTexCoords;

out vec2 TexCoords;

void main()
{
    gl_Position = vec4(aPos.x, aPos.y, 0.0, 1.0);
    TexCoords = vec2(aTexCoords.x, 1-aTexCoords.y);
}
    
#END

#BEGIN fragment_shader

#version 330 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D gAlbedo;
uniform sampler2D gNormal;
uniform sampler2D gPosition;
uniform sampler2D gDepth;

struct Light { 
    vec3 Position;
    vec3 Color;
    float Power;
};

const int NR_LIGHTS = 4;
uniform Light lights[NR_LIGHTS];
uniform vec3 viewPos;

void main()
{
    vec4 albedo = texture(gAlbedo, TexCoords);
    vec3 normal = texture(gNormal, TexCoords).rgb;
    vec3 fragPosition = texture(gPosition, TexCoords).rgb;
    
    vec3 ambient = vec3(.3,.6,.7);
    vec3 lighting = albedo.rgb * ambient * 0.4;
    vec3 viewDir = normalize(viewPos - fragPosition);
    
    //point lights
    for (int i = 0; i < NR_LIGHTS; i++)
    {
        float distance = length(lights[i].Position - fragPosition);
        distance *= distance;
        vec3 lightDir = normalize(lights[i].Position - fragPosition);
        vec3 diffuse = max(dot(normal, lightDir), 0.0) * albedo.rgb * lights[i].Color * lights[i].Power;
        lighting += diffuse / distance;
    }
    //directional light
    vec3 surfaceToLight = normalize(vec3(39,143,0));
    vec3 diffuse = max(dot(normal, surfaceToLight), 0.0) * albedo.rgb * 0.8;
    lighting += diffuse;
    lighting = mix (lighting, ambient, clamp(albedo.a,0,1));
    
    if (normal == vec3(0,0,0))
    {
        lighting = vec3(.4,.7,.8);
    }
    FragColor = vec4(lighting,1);
}

#END