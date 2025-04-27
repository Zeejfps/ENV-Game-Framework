#version 330

layout(location = 0) in vec2 attr_Position;

uniform vec4 u_color;
uniform vec4 u_rect;
uniform mat4 u_vp;
uniform vec4 u_borderRadius;
uniform vec4 u_borderSize;
uniform vec4 u_borderColor;

out vec2 uvs;
out vec4 color;
out vec4 borderSize;
out vec4 borderColor;
out vec4 borderRadius;
out vec4 rectInPixels;

void main() {

    // Assing out variables
    uvs = attr_Position;
    color = u_color;
    borderSize = u_borderSize;
    borderColor = u_borderColor;
    borderRadius = u_borderRadius;
    rectInPixels = u_rect;

    vec2 scaledPosition = u_rect.xy + attr_Position * u_rect.zw;
    vec4 position = vec4(scaledPosition.x, scaledPosition.y, 0, 1);
    gl_Position = u_vp * position;
}
