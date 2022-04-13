#version 330 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D gAlbedo;
uniform sampler2D gNormal;
uniform sampler2D gPosition;

struct Light { 
    vec3 Position;
    vec3 Color;
    float Power;
};

const int NR_LIGHTS = 1;
uniform Light lights[NR_LIGHTS];
uniform vec3 viewPos;

void main()
{
    vec3 albedo = texture(gAlbedo, TexCoords).rgb;
    vec3 normal = texture(gNormal, TexCoords).rgb;
    vec3 fragPosition = texture(gPosition, TexCoords).rgb;
    
    vec3 lighting = albedo * 0.1;
    vec3 viewDir = normalize(viewPos - fragPosition);
    
    for (int i = 0; i < NR_LIGHTS; i++)
    {
        float distance = length(lights[i].Position - fragPosition);
        vec3 lightDir = normalize(lights[i].Position - fragPosition);
        vec3 diffuse = max(dot(normal, lightDir), 0.0) * albedo * lights[i].Color * lights[i].Power;
        lighting += diffuse / distance;
    }
    if (normal == vec3(0,0,0))
    {
        discard;
    }
    FragColor = vec4(lighting,1);
}