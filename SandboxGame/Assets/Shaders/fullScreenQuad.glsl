#BEGIN vertex_shader

#version 460 core
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

#version 460 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D gAlbedo;
uniform sampler2D gNormal;
uniform sampler2D gPosition;
uniform sampler2D gDepth;
uniform sampler2D gMetallicRoughnessAo;

struct Light {
    vec3 Position;
    vec3 Color;
    float Power;
};

struct Material {
    sampler2D hdri;
};

const int NR_LIGHTS = 4;
uniform Light lights[NR_LIGHTS];
uniform vec3 viewPos;

layout (location = 0) uniform Material material;

vec3 FresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
}

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;

    float numerator = a2;
    float denominator = (NdotH2 * (a2 - 1.0) + 1.0);
    denominator = 3.14159265359 * denominator * denominator;

    return numerator / denominator;
}

vec3 FresnelSchlickRoughness(float cosTheta, vec3 F0, float roughness)
{
    return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(1.0 - cosTheta, 5.0);
}

vec2 DirectionToSphericalUV(vec3 direction)
{
    vec2 uv;
    uv.x = atan(direction.z, direction.x) / (2.0 * 3.14159265359) + 0.5;
    uv.y = asin(direction.y) / 3.14159265359 + 0.5;
    return uv;
}

vec3 SampleHDRI(vec3 direction, float roughness)
{
    vec2 uv = DirectionToSphericalUV(direction);
    return textureLod(material.hdri, uv, roughness * 4.0).rgb;
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r * r) / 8.0;

    float numerator = NdotV;
    float denominator = NdotV * (1.0 - k) + k;

    return numerator / denominator;
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}

vec3 CalcBRDF(vec3 N, vec3 V, vec3 L, vec3 albedo, float roughness, float metallic)
{
    vec3 H = normalize(V + L);
    vec3 F0 = vec3(0.04);
    F0 = mix(F0, albedo, metallic);

    vec3 F = FresnelSchlick(max(dot(H, V), 0.0), F0);
    float D = DistributionGGX(N, H , roughness);
    float G = GeometrySmith(N, V, L, roughness);

    vec3 numerator = D * G * F;
    float denominator = 4 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.001;
    vec3 specular = numerator / denominator;

    vec3 kS = F;
    vec3 kD = 1.0 - kS;
    kD *= 1.0 - metallic;

    float NdotL = max(dot(N, L), 0.0);
    return (kD * albedo / 3.14159265359 + specular) * NdotL;
}


void main()
{
    vec4 albedo = texture(gAlbedo, TexCoords);
    vec3 normal = texture(gNormal, TexCoords).rgb;
    vec3 fragPosition = texture(gPosition, TexCoords).rgb;
    float roughness = texture(gMetallicRoughnessAo, TexCoords).r;
    float metallic = texture(gMetallicRoughnessAo, TexCoords).g;

    vec3 viewDir = normalize(viewPos - fragPosition);
    vec3 lighting = vec3(0, 0, 0);

    // Point lights
    for (int i = 0; i < NR_LIGHTS; i++)
    {
        vec3 lightDir = normalize(lights[i].Position - fragPosition);
        vec3 H = normalize(viewDir + lightDir);
        vec3 F0 = vec3(0.04);
        F0 = mix(F0, albedo.rgb, metallic);
        vec3 F = FresnelSchlick(max(dot(H, viewDir), 0.0), F0);
        float D = DistributionGGX(normal, H, roughness);
        float NdotV = max(dot(normal, viewDir), 0.0);
        float NdotL = max(dot(normal, lightDir), 0.0);
        float G = GeometrySchlickGGX(NdotV, roughness) * GeometrySchlickGGX(NdotL, roughness);

        lighting += (albedo.rgb / 3.14159265359 + F * G * D) * lights[i].Color * NdotL * lights[i].Power / pow(distance(lights[i].Position, fragPosition), 2.0);
    }

    // Directional light
    vec3 surfaceToLight = normalize(vec3(39,143,0));
    vec3 H = normalize(viewDir + surfaceToLight);
    vec3 F0 = vec3(0.04);
    F0 = mix(F0, albedo.rgb, metallic);
    vec3 F = FresnelSchlick(max(dot(H, viewDir), 0.0), F0);
    float D = DistributionGGX(normal, H, roughness);
    float NdotV = max(dot(normal, viewDir), 0.0);
    float NdotL = max(dot(normal, surfaceToLight), 0.0);
    float G = GeometrySchlickGGX(NdotV, roughness) * GeometrySchlickGGX(NdotL, roughness);

    lighting += (albedo.rgb / 3.14159265359 + F * G * D) * NdotL;

    //------------ Add back HDRI environment map -----------//
    vec3 R = reflect(-viewDir, normal);
    vec3 irradiance = SampleHDRI(normal, 0.0);
    vec3 reflection = SampleHDRI(R, roughness);
    vec3 specularIBL = reflection * F * G * D / max(4.0 * NdotV * NdotL, 0.001);
    lighting += (albedo.rgb / 3.14159265359) * irradiance + specularIBL;

    FragColor = vec4(lighting, 1.0);
}
#END
