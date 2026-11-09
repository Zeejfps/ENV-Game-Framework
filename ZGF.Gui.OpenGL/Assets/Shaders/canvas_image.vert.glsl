// Generated from ZGF.Gui.Shaders/canvas_image.slang by ZGF.Gui.Shaders/regenerate.cs. Edit the .slang and regenerate.
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
out vec2 entryPointParam_vertexMain_uv_0;


#line 24
layout(location = 2)
out vec2 entryPointParam_vertexMain_local_0;


#line 24
flat layout(location = 3)
out vec2 entryPointParam_vertexMain_halfSize_0;


#line 24
flat layout(location = 4)
out vec4 entryPointParam_vertexMain_srcUV_0;


#line 24
flat layout(location = 5)
out float entryPointParam_vertexMain_fadesEdges_0;


#line 24
flat layout(location = 6)
out uint entryPointParam_vertexMain_tint_0;


#line 24
flat layout(location = 7)
out uint entryPointParam_vertexMain_clipIndex_0;


#line 24
flat layout(location = 8)
out vec4 entryPointParam_vertexMain_cornerRadius_0;


#line 24
layout(location = 0)
in vec2 input_unitPos_0;


#line 24
layout(location = 1)
in vec4 input_rect_0;


#line 24
layout(location = 2)
in vec4 input_srcUV_0;


#line 24
layout(location = 3)
in uint input_tint_0;


#line 24
layout(location = 4)
in uint input_clipIndex_0;


#line 24
layout(location = 5)
in float input_rotation_0;


#line 24
layout(location = 6)
in vec4 input_cornerRadius_0;


#line 16 1
struct Varyings_0
{
    vec4 position_0;
    vec2 pixelPos_1;
    vec2 uv_0;
    vec2 local_0;
    vec2 halfSize_0;
    vec4 srcUV_0;
    float fadesEdges_0;
    uint tint_0;
    uint clipIndex_0;
    vec4 cornerRadius_0;
};


#line 34
void main()
{



    vec2 devicePerLogical_0 = Globals_0.canvasMetrics_0.zw / Globals_0.canvasMetrics_0.xy;
    vec2 low_0 = input_rect_0.xy * devicePerLogical_0;
    vec2 high_0 = (input_rect_0.xy + input_rect_0.zw) * devicePerLogical_0;
    vec2 _S1 = max(abs(low_0 - round(low_0)), abs(high_0 - round(high_0)));

#line 42
    bool growable_0;
    if(input_rotation_0 == 0.0)
    {

#line 43
        growable_0 = (input_rect_0.z) > 0.0;

#line 43
    }
    else
    {

#line 43
        growable_0 = false;

#line 43
    }

#line 43
    if(growable_0)
    {

#line 43
        growable_0 = (input_rect_0.w) > 0.0;

#line 43
    }
    else
    {

#line 43
        growable_0 = false;

#line 43
    }

#line 43
    vec2 outset_0;
    if(growable_0)
    {

#line 44
        outset_0 = step(vec2(0.00390625), _S1) / devicePerLogical_0;

#line 44
    }
    else
    {

#line 44
        outset_0 = vec2(0.0);

#line 44
    }
    vec2 unit_0 = (input_unitPos_0 * (input_rect_0.zw + 2.0 * outset_0) - outset_0) / max(input_rect_0.zw, vec2(9.99999997475242708e-07));


    vec2 local_1 = (unit_0 - vec2(0.5)) * input_rect_0.zw;
    float cs_0 = cos(input_rotation_0);
    float sn_0 = sin(input_rotation_0);
    float _S2 = local_1.x;

#line 51
    float _S3 = local_1.y;
    vec2 pixelPos_2 = input_rect_0.xy + input_rect_0.zw * 0.5 + vec2(cs_0 * _S2 - sn_0 * _S3, sn_0 * _S2 + cs_0 * _S3);

    Varyings_0 o_0;
    o_0.position_0 = project_0(pixelPos_2);
    o_0.pixelPos_1 = pixelPos_2;

    o_0.uv_0 = vec2(input_srcUV_0.x + unit_0.x * input_srcUV_0.z, input_srcUV_0.y + unit_0.y * input_srcUV_0.w);


    o_0.local_0 = local_1;
    o_0.halfSize_0 = input_rect_0.zw * 0.5;
    o_0.srcUV_0 = input_srcUV_0;
    if((outset_0.x) > 0.0)
    {

#line 64
        growable_0 = true;

#line 64
    }
    else
    {

#line 64
        growable_0 = (outset_0.y) > 0.0;

#line 64
    }

#line 64
    float _S4;

#line 64
    if(growable_0)
    {

#line 64
        _S4 = 1.0;

#line 64
    }
    else
    {

#line 64
        _S4 = 0.0;

#line 64
    }

#line 64
    o_0.fadesEdges_0 = _S4;
    o_0.tint_0 = input_tint_0;
    o_0.clipIndex_0 = input_clipIndex_0;
    o_0.cornerRadius_0 = min(input_cornerRadius_0, vec4(min(input_rect_0.z, input_rect_0.w) * 0.5));
    Varyings_0 _S5 = o_0;

#line 68
    gl_Position = o_0.position_0;

#line 68
    entryPointParam_vertexMain_pixelPos_0 = _S5.pixelPos_1;

#line 68
    entryPointParam_vertexMain_uv_0 = _S5.uv_0;

#line 68
    entryPointParam_vertexMain_local_0 = _S5.local_0;

#line 68
    entryPointParam_vertexMain_halfSize_0 = _S5.halfSize_0;

#line 68
    entryPointParam_vertexMain_srcUV_0 = _S5.srcUV_0;

#line 68
    entryPointParam_vertexMain_fadesEdges_0 = _S5.fadesEdges_0;

#line 68
    entryPointParam_vertexMain_tint_0 = _S5.tint_0;

#line 68
    entryPointParam_vertexMain_clipIndex_0 = _S5.clipIndex_0;

#line 68
    entryPointParam_vertexMain_cornerRadius_0 = _S5.cornerRadius_0;

#line 68
    return;
}

