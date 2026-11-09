// Generated from ZGF.Gui.Shaders/canvas_shape.slang by ZGF.Gui.Shaders/regenerate.cs. Edit the .slang and regenerate.
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
out vec4 entryPointParam_vertexMain_shapeData_0;


#line 24
layout(location = 2)
out vec4 entryPointParam_vertexMain_shapeData2_0;


#line 24
flat layout(location = 3)
out float entryPointParam_vertexMain_halfWidth_0;


#line 24
flat layout(location = 4)
out uint entryPointParam_vertexMain_color_0;


#line 24
flat layout(location = 5)
out uint entryPointParam_vertexMain_shapeType_0;


#line 24
flat layout(location = 6)
out uint entryPointParam_vertexMain_clipIndex_0;


#line 24
flat layout(location = 7)
out uint entryPointParam_vertexMain_color2_0;


#line 24
flat layout(location = 8)
out uint entryPointParam_vertexMain_flags_0;


#line 24
layout(location = 0)
in vec2 input_unitPos_0;


#line 24
layout(location = 1)
in vec4 input_outerRect_0;


#line 24
layout(location = 2)
in vec4 input_shapeData_0;


#line 24
layout(location = 3)
in vec4 input_shapeData2_0;


#line 24
layout(location = 4)
in float input_halfWidth_0;


#line 24
layout(location = 5)
in uint input_color_0;


#line 24
layout(location = 6)
in uint input_shapeType_0;


#line 24
layout(location = 7)
in uint input_clipIndex_0;


#line 24
layout(location = 8)
in uint input_color2_0;


#line 24
layout(location = 9)
in uint input_flags_0;


#line 19 1
struct Varyings_0
{
    vec4 position_0;
    vec2 pixelPos_1;
    vec4 shapeData_0;
    vec4 shapeData2_0;
    float halfWidth_0;
    uint color_0;
    uint shapeType_0;
    uint clipIndex_0;
    uint color2_0;
    uint flags_0;
};


void main()
{
    vec2 pixelPos_2 = input_outerRect_0.xy + input_unitPos_0 * input_outerRect_0.zw;

    Varyings_0 o_0;
    o_0.position_0 = project_0(pixelPos_2);
    o_0.pixelPos_1 = pixelPos_2;
    o_0.shapeData_0 = input_shapeData_0;
    o_0.shapeData2_0 = input_shapeData2_0;
    o_0.halfWidth_0 = input_halfWidth_0;
    o_0.color_0 = input_color_0;
    o_0.shapeType_0 = input_shapeType_0;
    o_0.clipIndex_0 = input_clipIndex_0;
    o_0.color2_0 = input_color2_0;
    o_0.flags_0 = input_flags_0;
    Varyings_0 _S1 = o_0;

#line 49
    gl_Position = o_0.position_0;

#line 49
    entryPointParam_vertexMain_pixelPos_0 = _S1.pixelPos_1;

#line 49
    entryPointParam_vertexMain_shapeData_0 = _S1.shapeData_0;

#line 49
    entryPointParam_vertexMain_shapeData2_0 = _S1.shapeData2_0;

#line 49
    entryPointParam_vertexMain_halfWidth_0 = _S1.halfWidth_0;

#line 49
    entryPointParam_vertexMain_color_0 = _S1.color_0;

#line 49
    entryPointParam_vertexMain_shapeType_0 = _S1.shapeType_0;

#line 49
    entryPointParam_vertexMain_clipIndex_0 = _S1.clipIndex_0;

#line 49
    entryPointParam_vertexMain_color2_0 = _S1.color2_0;

#line 49
    entryPointParam_vertexMain_flags_0 = _S1.flags_0;

#line 49
    return;
}

