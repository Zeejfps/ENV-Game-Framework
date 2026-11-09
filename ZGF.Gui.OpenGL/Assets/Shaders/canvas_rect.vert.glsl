// Generated from ZGF.Gui.Shaders/canvas_rect.slang by ZGF.Gui.Shaders/regenerate.cs. Edit the .slang and regenerate.
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
flat layout(location = 0)
out vec2 entryPointParam_vertexMain_rectOrigin_0;


#line 24
flat layout(location = 1)
out vec4 entryPointParam_vertexMain_rectSize_0;


#line 24
flat layout(location = 2)
out vec4 entryPointParam_vertexMain_borderRadius_0;


#line 24
flat layout(location = 3)
out vec4 entryPointParam_vertexMain_borderSize_0;


#line 24
flat layout(location = 4)
out uint entryPointParam_vertexMain_bgColor_0;


#line 24
flat layout(location = 5)
out uint entryPointParam_vertexMain_borderColorTop_0;


#line 24
flat layout(location = 6)
out uint entryPointParam_vertexMain_borderColorRight_0;


#line 24
flat layout(location = 7)
out uint entryPointParam_vertexMain_borderColorBottom_0;


#line 24
flat layout(location = 8)
out uint entryPointParam_vertexMain_borderColorLeft_0;


#line 24
flat layout(location = 9)
out uint entryPointParam_vertexMain_clipIndex_0;


#line 24
flat layout(location = 10)
out float entryPointParam_vertexMain_fadesEdges_0;


#line 24
layout(location = 0)
in vec2 input_unitPos_0;


#line 24
layout(location = 1)
in vec4 input_rect_0;


#line 24
layout(location = 2)
in vec4 input_borderRadius_0;


#line 24
layout(location = 3)
in vec4 input_borderSize_0;


#line 24
layout(location = 4)
in uint input_bgColor_0;


#line 24
layout(location = 5)
in uint input_borderColorTop_0;


#line 24
layout(location = 6)
in uint input_borderColorRight_0;


#line 24
layout(location = 7)
in uint input_borderColorBottom_0;


#line 24
layout(location = 8)
in uint input_borderColorLeft_0;


#line 24
layout(location = 9)
in uint input_clipIndex_0;


#line 17 1
struct Varyings_0
{
    vec4 position_0;
    vec2 rectOrigin_0;
    vec4 rectSize_0;
    vec4 borderRadius_0;
    vec4 borderSize_0;
    uint bgColor_0;
    uint borderColorTop_0;
    uint borderColorRight_0;
    uint borderColorBottom_0;
    uint borderColorLeft_0;
    uint clipIndex_0;
    float fadesEdges_0;
};



void main()
{

#line 41
    vec2 devicePerLogical_0 = Globals_0.canvasMetrics_0.zw / Globals_0.canvasMetrics_0.xy;
    vec2 low_0 = input_rect_0.xy * devicePerLogical_0;
    vec2 high_0 = (input_rect_0.xy + input_rect_0.zw) * devicePerLogical_0;
    vec2 _S1 = max(abs(low_0 - round(low_0)), abs(high_0 - round(high_0)));

#line 44
    bool hasArea_0;

    if((input_rect_0.z) > 0.0)
    {

#line 46
        hasArea_0 = (input_rect_0.w) > 0.0;

#line 46
    }
    else
    {

#line 46
        hasArea_0 = false;

#line 46
    }

#line 46
    vec2 outset_0;
    if(hasArea_0)
    {

#line 47
        outset_0 = step(vec2(0.00390625), _S1) / devicePerLogical_0;

#line 47
    }
    else
    {

#line 47
        outset_0 = vec2(0.0);

#line 47
    }

    Varyings_0 o_0;
    if((outset_0.x) > 0.0)
    {

#line 50
        hasArea_0 = true;

#line 50
    }
    else
    {

#line 50
        hasArea_0 = (outset_0.y) > 0.0;

#line 50
    }

#line 50
    float _S2;

#line 50
    if(hasArea_0)
    {

#line 50
        _S2 = 1.0;

#line 50
    }
    else
    {

#line 50
        _S2 = 0.0;

#line 50
    }

#line 50
    o_0.fadesEdges_0 = _S2;

    o_0.position_0 = project_0(input_rect_0.xy - outset_0 + input_unitPos_0 * (input_rect_0.zw + 2.0 * outset_0));
    o_0.rectOrigin_0 = input_rect_0.xy;
    o_0.rectSize_0 = vec4(input_rect_0.zw, input_rect_0.zw);
    o_0.borderRadius_0 = input_borderRadius_0;
    o_0.borderSize_0 = input_borderSize_0;
    o_0.bgColor_0 = input_bgColor_0;
    o_0.borderColorTop_0 = input_borderColorTop_0;
    o_0.borderColorRight_0 = input_borderColorRight_0;
    o_0.borderColorBottom_0 = input_borderColorBottom_0;
    o_0.borderColorLeft_0 = input_borderColorLeft_0;
    o_0.clipIndex_0 = input_clipIndex_0;
    Varyings_0 _S3 = o_0;

#line 63
    gl_Position = o_0.position_0;

#line 63
    entryPointParam_vertexMain_rectOrigin_0 = _S3.rectOrigin_0;

#line 63
    entryPointParam_vertexMain_rectSize_0 = _S3.rectSize_0;

#line 63
    entryPointParam_vertexMain_borderRadius_0 = _S3.borderRadius_0;

#line 63
    entryPointParam_vertexMain_borderSize_0 = _S3.borderSize_0;

#line 63
    entryPointParam_vertexMain_bgColor_0 = _S3.bgColor_0;

#line 63
    entryPointParam_vertexMain_borderColorTop_0 = _S3.borderColorTop_0;

#line 63
    entryPointParam_vertexMain_borderColorRight_0 = _S3.borderColorRight_0;

#line 63
    entryPointParam_vertexMain_borderColorBottom_0 = _S3.borderColorBottom_0;

#line 63
    entryPointParam_vertexMain_borderColorLeft_0 = _S3.borderColorLeft_0;

#line 63
    entryPointParam_vertexMain_clipIndex_0 = _S3.clipIndex_0;

#line 63
    entryPointParam_vertexMain_fadesEdges_0 = _S3.fadesEdges_0;

#line 63
    return;
}

