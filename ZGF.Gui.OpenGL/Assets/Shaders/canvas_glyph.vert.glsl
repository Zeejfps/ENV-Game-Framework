// Generated from ZGF.Gui.Shaders/canvas_glyph.slang by ZGF.Gui.Shaders/regenerate.cs. Edit the .slang and regenerate.
#version 450
layout(row_major) uniform;
layout(row_major) buffer;

#line 4 0
struct GlobalsData_0
{
    mat4x4 projection_0;
    vec4 canvasMetrics_0;
};


layout(binding = 0)
layout(std140) uniform block_GlobalsData_0
{
    mat4x4 projection_0;
    vec4 canvasMetrics_0;
}Globals_0;

#line 22
vec4 project_0(vec2 pixelPos_0)
{
    return (((vec4(pixelPos_0, 0.0, 1.0)) * (Globals_0.projection_0)));
}


#line 24
layout(location = 0)
out vec2 entryPointParam_vertexMain_pixelPos_0;


#line 24
layout(location = 1)
out vec2 entryPointParam_vertexMain_atlasUV_0;


#line 24
flat layout(location = 2)
out uint entryPointParam_vertexMain_color_0;


#line 24
flat layout(location = 3)
out uint entryPointParam_vertexMain_clipIndex_0;


#line 24
layout(location = 0)
in vec2 input_unitPos_0;


#line 24
layout(location = 1)
in vec4 input_rect_0;


#line 24
layout(location = 2)
in vec4 input_atlasUV_0;


#line 24
layout(location = 3)
in uint input_color_0;


#line 24
layout(location = 4)
in uint input_clipIndex_0;


#line 24
layout(location = 5)
in float input_rotation_0;


#line 15 1
struct Varyings_0
{
    vec4 position_0;
    vec2 pixelPos_1;
    vec2 atlasUV_0;
    uint color_0;
    uint clipIndex_0;
};


void main()
{

    vec2 local_0 = (input_unitPos_0 - vec2(0.5)) * input_rect_0.zw;
    float cs_0 = cos(input_rotation_0);
    float sn_0 = sin(input_rotation_0);
    float _S1 = local_0.x;

#line 31
    float _S2 = local_0.y;
    vec2 pixelPos_2 = input_rect_0.xy + input_rect_0.zw * 0.5 + vec2(cs_0 * _S1 - sn_0 * _S2, sn_0 * _S1 + cs_0 * _S2);

    Varyings_0 o_0;
    o_0.position_0 = project_0(pixelPos_2);
    o_0.pixelPos_1 = pixelPos_2;


    o_0.atlasUV_0 = vec2(input_atlasUV_0.x + input_unitPos_0.x * input_atlasUV_0.z, input_atlasUV_0.y + input_unitPos_0.y * input_atlasUV_0.w);


    o_0.color_0 = input_color_0;
    o_0.clipIndex_0 = input_clipIndex_0;
    Varyings_0 _S3 = o_0;

#line 44
    gl_Position = o_0.position_0;

#line 44
    entryPointParam_vertexMain_pixelPos_0 = _S3.pixelPos_1;

#line 44
    entryPointParam_vertexMain_atlasUV_0 = _S3.atlasUV_0;

#line 44
    entryPointParam_vertexMain_color_0 = _S3.color_0;

#line 44
    entryPointParam_vertexMain_clipIndex_0 = _S3.clipIndex_0;

#line 44
    return;
}

