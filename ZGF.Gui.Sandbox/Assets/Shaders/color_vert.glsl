#version 400

layout(location = 0) in vec3 position;
layout(location = 1) in vec3 a_normal;

uniform mat4 model_matrix;
uniform mat4 view_projection_matrix;

out vec3 v_color;

void main() {

    // --- 1. Hardcode Light Properties ---
    // We define the light's position in world space.
    // You can move this around to see the lighting change.
    vec3 lightPos_world = vec3(10.0, 10.0, 10.0);

    // Basic material and light properties
    vec3 objectColor = vec3(1.0, 0.5, 0.2); // An orange-ish color
    vec3 lightColor = vec3(1.0, 1.0, 1.0);  // A standard white light
    float ambientStrength = 0.2;            // A little bit of ambient light

    // --- 2. Perform Calculations in World Space ---
    // Transform vertex position to world space
    vec3 fragPos_world = vec3(model_matrix * vec4(position, 1.0));
    
    // Transform normal to world space and normalize it.
    // We use mat3(model_matrix) to correctly handle rotation.
    // Note: This can be skewed by non-uniform scaling. For that, you'd need transpose(inverse(model_matrix)).
    vec3 normal_world = normalize(mat3(model_matrix) * a_normal);

    // --- 3. Calculate Gouraud Shading ---
    // Ambient component
    vec3 ambient = ambientStrength * lightColor;

    // Diffuse component
    vec3 lightDir = normalize(lightPos_world - fragPos_world);
    float diff = max(dot(normal_world, lightDir), 0.0);
    vec3 diffuse = diff * lightColor;

    // Final color for THIS VERTEX is the combination of ambient and diffuse
    // light, multiplied by the object's own color.
    v_color = (ambient + diffuse) * objectColor;

    // --- 4. Final Vertex Position ---
    // The gl_Position calculation remains, but we can reuse our world position.
    gl_Position = view_projection_matrix * vec4(fragPos_world, 1.0);
}