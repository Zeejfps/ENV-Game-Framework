#BEGIN vertex_shader

#version 460
layout (location = 0) in vec3 attr_VertexPosition;

void main()
{
    gl_Position = vec4(attr_VertexPosition, 1);
}

#END

#BEGIN fragment_shader
    
#version 460

uniform vec4 u_GridColor = vec4(0.4, 0.7, 0.4, 0.5);
uniform vec4 u_BackgroundColor = vec4(0, 0.3, 0, 1);
uniform vec2 u_Pitch  = vec2(20, 20);

layout(location = 0) out vec4 out_FragColor;

void main() {
    if (int(mod(gl_FragCoord.x, u_Pitch[0])) == 0 || int(mod(gl_FragCoord.y, u_Pitch[1])) == 0) {
        out_FragColor = u_GridColor;
    } else {
        out_FragColor = u_BackgroundColor;
    }
}

#END