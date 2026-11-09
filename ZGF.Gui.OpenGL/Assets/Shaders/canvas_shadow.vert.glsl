// Generated from ZGF.Gui.Shaders/canvas_shadow.slang by ZGF.Gui.Shaders/regenerate.cs. Edit the .slang and regenerate.
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
out vec4 entryPointParam_vertexMain_shadowRect_0;


#line 24
layout(location = 2)
out vec4 entryPointParam_vertexMain_borderRadius_0;


#line 24
flat layout(location = 3)
out float entryPointParam_vertexMain_sigma_0;


#line 24
flat layout(location = 4)
out uint entryPointParam_vertexMain_color_0;


#line 24
flat layout(location = 5)
out uint entryPointParam_vertexMain_clipIndex_0;


#line 24
layout(location = 0)
in vec2 input_unitPos_0;


#line 24
layout(location = 1)
in vec4 input_outerRect_0;


#line 24
layout(location = 2)
in vec4 input_shadowRect_0;


#line 24
layout(location = 3)
in vec4 input_borderRadius_0;


#line 24
layout(location = 4)
in float input_sigma_0;


#line 24
layout(location = 5)
in uint input_color_0;


#line 24
layout(location = 6)
in uint input_clipIndex_0;


#line 18 1
struct Varyings_0
{
    vec4 position_0;
    vec2 pixelPos_1;
    vec4 shadowRect_0;
    vec4 borderRadius_0;
    float sigma_0;
    uint color_0;
    uint clipIndex_0;
};


void main()
{
    vec2 pixelPos_2 = input_outerRect_0.xy + input_unitPos_0 * input_outerRect_0.zw;

    Varyings_0 o_0;
    o_0.position_0 = project_0(pixelPos_2);
    o_0.pixelPos_1 = pixelPos_2;
    o_0.shadowRect_0 = input_shadowRect_0;
    o_0.borderRadius_0 = input_borderRadius_0;
    o_0.sigma_0 = input_sigma_0;
    o_0.color_0 = input_color_0;
    o_0.clipIndex_0 = input_clipIndex_0;
    Varyings_0 _S1 = o_0;

#line 42
    gl_Position = o_0.position_0;

#line 42
    entryPointParam_vertexMain_pixelPos_0 = _S1.pixelPos_1;

#line 42
    entryPointParam_vertexMain_shadowRect_0 = _S1.shadowRect_0;

#line 42
    entryPointParam_vertexMain_borderRadius_0 = _S1.borderRadius_0;

#line 42
    entryPointParam_vertexMain_sigma_0 = _S1.sigma_0;

#line 42
    entryPointParam_vertexMain_color_0 = _S1.color_0;

#line 42
    entryPointParam_vertexMain_clipIndex_0 = _S1.clipIndex_0;

#line 42
    return;
}

