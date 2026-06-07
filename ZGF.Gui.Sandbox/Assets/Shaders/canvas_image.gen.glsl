#version 450
layout(column_major) uniform;
layout(column_major) buffer;

#line 23 0
struct SLANG_ParameterGroup_Globals_0
{
    mat4x4 u_projection_0;
};


#line 23
layout(binding = 0)
layout(std140) uniform block_SLANG_ParameterGroup_Globals_0
{
    mat4x4 u_projection_0;
}Globals_0;
struct SLANG_ParameterGroup_ClipRects_0
{
    vec4  u_clipRects_0[256];
};


#line 28
layout(binding = 1)
layout(std140) uniform block_SLANG_ParameterGroup_ClipRects_0
{
    vec4  u_clipRects_0[256];
}ClipRects_0;
layout(binding = 2)
uniform sampler2D u_texture_0;


#line 11
layout(location = 0)
out vec2 entryPointParam_vertexMain_pixelPos_0;


#line 11
layout(location = 1)
out vec2 entryPointParam_vertexMain_uv_0;


#line 11
flat layout(location = 2)
out uint entryPointParam_vertexMain_tint_0;


#line 11
flat layout(location = 3)
out uint entryPointParam_vertexMain_clipIndex_0;


#line 11
layout(location = 0)
in vec2 input_unitPos_0;


#line 11
layout(location = 1)
in vec4 input_rect_0;


#line 11
layout(location = 2)
in vec4 input_srcUV_0;


#line 11
layout(location = 3)
in uint input_tint_0;


#line 11
layout(location = 4)
in uint input_clipIndex_0;


#line 11
layout(location = 5)
in float input_rotation_0;

struct Varyings_0
{
    vec4 position_0;
    vec2 pixelPos_0;
    vec2 uv_0;
    uint tint_0;
    uint clipIndex_0;
};


#line 36
void main()
{


    vec2 local_0 = (input_unitPos_0 - vec2(0.5, 0.5)) * input_rect_0.zw;
    float cs_0 = cos(input_rotation_0);
    float sn_0 = sin(input_rotation_0);
    float _S1 = local_0.x;

#line 43
    float _S2 = local_0.y;
    vec2 pixelPos_1 = input_rect_0.xy + input_rect_0.zw * 0.5 + vec2(cs_0 * _S1 - sn_0 * _S2, sn_0 * _S1 + cs_0 * _S2);

#line 38
    Varyings_0 o_0;

#line 45
    o_0.position_0 = (((vec4(pixelPos_1, 0.0, 1.0)) * (Globals_0.u_projection_0)));
    o_0.pixelPos_0 = pixelPos_1;
    o_0.uv_0 = vec2(input_srcUV_0.x + input_unitPos_0.x * input_srcUV_0.z, input_srcUV_0.y + input_unitPos_0.y * input_srcUV_0.w);


    o_0.tint_0 = input_tint_0;
    o_0.clipIndex_0 = input_clipIndex_0;
    Varyings_0 _S3 = o_0;

#line 52
    gl_Position = o_0.position_0;

#line 52
    entryPointParam_vertexMain_pixelPos_0 = _S3.pixelPos_0;

#line 52
    entryPointParam_vertexMain_uv_0 = _S3.uv_0;

#line 52
    entryPointParam_vertexMain_tint_0 = _S3.tint_0;

#line 52
    entryPointParam_vertexMain_clipIndex_0 = _S3.clipIndex_0;

#line 52
    return;
}

vec4 unpackARGB_0(uint c_0)
{

#line 61
    return vec4(float((c_0 >> 16) & 255U) / 255.0, float((c_0 >> 8) & 255U) / 255.0, float(c_0 & 255U) / 255.0, float((c_0 >> 24) & 255U) / 255.0);
}


#line 61
layout(location = 0)
out vec4 entryPointParam_fragmentMain_0;


#line 61
layout(location = 0)
in vec2 v_pixelPos_0;


#line 61
layout(location = 1)
in vec2 v_uv_0;


#line 61
flat layout(location = 2)
in uint v_tint_0;


#line 61
flat layout(location = 3)
in uint v_clipIndex_0;


void main()
{
    vec4 clip_0 = ClipRects_0.u_clipRects_0[v_clipIndex_0];

#line 67
    bool _S4;
    if((v_pixelPos_0.x) < (ClipRects_0.u_clipRects_0[v_clipIndex_0].x))
    {

#line 68
        _S4 = true;

#line 68
    }
    else
    {

#line 68
        _S4 = (v_pixelPos_0.x) >= (clip_0.z);

#line 68
    }

#line 68
    if(_S4)
    {

#line 68
        _S4 = true;

#line 68
    }
    else
    {

#line 68
        _S4 = (v_pixelPos_0.y) < (clip_0.y);

#line 68
    }
    if(_S4)
    {

#line 69
        _S4 = true;

#line 69
    }
    else
    {

#line 69
        _S4 = (v_pixelPos_0.y) >= (clip_0.w);

#line 69
    }

#line 68
    if(_S4)
    {

        discard;

#line 68
    }

#line 68
    entryPointParam_fragmentMain_0 = (texture((u_texture_0), (v_uv_0))) * unpackARGB_0(v_tint_0);

#line 68
    return;
}

