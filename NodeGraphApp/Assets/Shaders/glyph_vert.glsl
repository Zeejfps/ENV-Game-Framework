#version 330

layout(location = 0) in vec2 attr_Position;

uniform vec4 u_rect;
uniform vec4 u_spriteRect;
uniform mat4 u_viewProjMat;

out vec2 TexCoords;

void main() {
    TexCoords = attr_Position;
    TexCoords.x = TexCoords.x * u_spriteRect.z + u_spriteRect.x;
    TexCoords.y = (1.0 - TexCoords.y) * u_spriteRect.w + u_spriteRect.y;
    
    vec2 scaledPosition = u_rect.xy + attr_Position * u_rect.zw;
    vec4 position = vec4(scaledPosition.x, scaledPosition.y, 0, 1);
    gl_Position = u_viewProjMat * position;
}
