#version 450
layout(column_major) uniform;
layout(column_major) buffer;

#line 38 0
struct SLANG_ParameterGroup_Globals_0
{
    mat4x4 u_projection_0;
};


#line 38
layout(binding = 0)
layout(std140) uniform block_SLANG_ParameterGroup_Globals_0
{
    mat4x4 u_projection_0;
}Globals_0;
struct SLANG_ParameterGroup_ClipRects_0
{
    vec4  u_clipRects_0[256];
};


#line 43
layout(binding = 1)
layout(std140) uniform block_SLANG_ParameterGroup_ClipRects_0
{
    vec4  u_clipRects_0[256];
}ClipRects_0;

#line 24
layout(location = 0)
out vec2 entryPointParam_vertexMain_pixelPos_0;


#line 24
layout(location = 1)
out vec4 entryPointParam_vertexMain_shapeData_0;


#line 24
flat layout(location = 2)
out float entryPointParam_vertexMain_halfWidth_0;


#line 24
flat layout(location = 3)
out uint entryPointParam_vertexMain_color_0;


#line 24
flat layout(location = 4)
out uint entryPointParam_vertexMain_shapeType_0;


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
in vec4 input_shapeData_0;


#line 24
layout(location = 3)
in float input_halfWidth_0;


#line 24
layout(location = 4)
in uint input_color_0;


#line 24
layout(location = 5)
in uint input_shapeType_0;


#line 24
layout(location = 6)
in uint input_clipIndex_0;

struct Varyings_0
{
    vec4 position_0;
    vec2 pixelPos_0;
    vec4 shapeData_0;
    float halfWidth_0;
    uint color_0;
    uint shapeType_0;
    uint clipIndex_0;
};


#line 49
void main()
{

    vec2 pixelPos_1 = input_outerRect_0.xy + input_unitPos_0 * input_outerRect_0.zw;

#line 51
    Varyings_0 o_0;

    o_0.position_0 = (((vec4(pixelPos_1, 0.0, 1.0)) * (Globals_0.u_projection_0)));
    o_0.pixelPos_0 = pixelPos_1;
    o_0.shapeData_0 = input_shapeData_0;
    o_0.halfWidth_0 = input_halfWidth_0;
    o_0.color_0 = input_color_0;
    o_0.shapeType_0 = input_shapeType_0;
    o_0.clipIndex_0 = input_clipIndex_0;
    Varyings_0 _S1 = o_0;

#line 60
    gl_Position = o_0.position_0;

#line 60
    entryPointParam_vertexMain_pixelPos_0 = _S1.pixelPos_0;

#line 60
    entryPointParam_vertexMain_shapeData_0 = _S1.shapeData_0;

#line 60
    entryPointParam_vertexMain_halfWidth_0 = _S1.halfWidth_0;

#line 60
    entryPointParam_vertexMain_color_0 = _S1.color_0;

#line 60
    entryPointParam_vertexMain_shapeType_0 = _S1.shapeType_0;

#line 60
    entryPointParam_vertexMain_clipIndex_0 = _S1.clipIndex_0;

#line 60
    return;
}


#line 73
float sdSegment_0(vec2 p_0, vec2 a_0, vec2 b_0)
{
    vec2 pa_0 = p_0 - a_0;
    vec2 ba_0 = b_0 - a_0;

    return length(pa_0 - ba_0 * clamp(dot(pa_0, ba_0) / max(dot(ba_0, ba_0), 9.99999997475242708e-07), 0.0, 1.0));
}


#line 63
vec4 unpackARGB_0(uint c_0)
{

#line 69
    return vec4(float((c_0 >> 16) & 255U) / 255.0, float((c_0 >> 8) & 255U) / 255.0, float(c_0 & 255U) / 255.0, float((c_0 >> 24) & 255U) / 255.0);
}


#line 69
layout(location = 0)
out vec4 entryPointParam_fragmentMain_0;


#line 69
layout(location = 0)
in vec2 v_pixelPos_0;


#line 69
layout(location = 1)
in vec4 v_shapeData_0;


#line 69
flat layout(location = 2)
in float v_halfWidth_0;


#line 69
flat layout(location = 3)
in uint v_color_0;


#line 69
flat layout(location = 4)
in uint v_shapeType_0;


#line 69
flat layout(location = 5)
in uint v_clipIndex_0;


#line 82
void main()
{
    vec4 clip_0 = ClipRects_0.u_clipRects_0[v_clipIndex_0];

#line 84
    bool _S2;
    if((v_pixelPos_0.x) < (ClipRects_0.u_clipRects_0[v_clipIndex_0].x))
    {

#line 85
        _S2 = true;

#line 85
    }
    else
    {

#line 85
        _S2 = (v_pixelPos_0.x) >= (clip_0.z);

#line 85
    }

#line 85
    if(_S2)
    {

#line 85
        _S2 = true;

#line 85
    }
    else
    {

#line 85
        _S2 = (v_pixelPos_0.y) < (clip_0.y);

#line 85
    }
    if(_S2)
    {

#line 86
        _S2 = true;

#line 86
    }
    else
    {

#line 86
        _S2 = (v_pixelPos_0.y) >= (clip_0.w);

#line 86
    }

#line 85
    if(_S2)
    {

        discard;

#line 85
    }

#line 85
    float d_0;

#line 92
    if(v_shapeType_0 == 2U)
    {

#line 92
        d_0 = sdSegment_0(v_pixelPos_0, v_shapeData_0.xy, v_shapeData_0.zw) - v_halfWidth_0;

#line 92
    }
    else
    {


        if(v_shapeType_0 == 1U)
        {

#line 97
            d_0 = abs(length(v_pixelPos_0 - v_shapeData_0.xy) - v_shapeData_0.z) - v_halfWidth_0;

#line 97
        }
        else
        {

#line 97
            d_0 = length(v_pixelPos_0 - v_shapeData_0.xy) - v_shapeData_0.z;

#line 97
        }

#line 92
    }

#line 110
    float coverage_0 = clamp(0.5 - d_0 / max((fwidth((d_0))), 9.99999997475242708e-07), 0.0, 1.0);
    if(coverage_0 <= 0.0)
    {
        discard;

#line 111
    }

#line 116
    vec4 rgba_0 = unpackARGB_0(v_color_0);

#line 116
    entryPointParam_fragmentMain_0 = vec4(rgba_0.xyz, rgba_0.w * coverage_0);

#line 116
    return;
}

