#BEGIN vertex_shader

#version 330 core

layout (location = 0) in vec4 position;
layout (location = 2) in vec2 texCoord;

out vec2 fragTexCoord;

void main()
{
    gl_Position = position;
    fragTexCoord = texCoord;
}
    
#END
    
#BEGIN fragment_shader
    
#version 330 core

in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D textureSampler;

void main()
{
    fragColor = texture(textureSampler, fragTexCoord);
}

#END
