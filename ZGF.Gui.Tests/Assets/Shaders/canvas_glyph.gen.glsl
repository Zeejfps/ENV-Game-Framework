#version 450
layout(column_major) uniform;
layout(column_major) buffer;

#line 22 0
struct SLANG_ParameterGroup_Globals_0
{
    mat4x4 u_projection_0;
};


#line 22
layout(binding = 0)
layout(std140) uniform block_SLANG_ParameterGroup_Globals_0
{
    mat4x4 u_projection_0;
}Globals_0;
struct SLANG_ParameterGroup_ClipRects_0
{
    vec4  u_clipRects_0[256];
};


#line 27
layout(binding = 1)
layout(std140) uniform block_SLANG_ParameterGroup_ClipRects_0
{
    vec4  u_clipRects_0[256];
}ClipRects_0;
layout(binding = 2)
uniform sampler2D u_atlas_0;


#line 10
layout(location = 0)
out vec2 entryPointParam_vertexMain_pixelPos_0;


#line 10
layout(location = 1)
out vec2 entryPointParam_vertexMain_atlasUV_0;


#line 10
flat layout(location = 2)
out uint entryPointParam_vertexMain_color_0;


#line 10
flat layout(location = 3)
out uint entryPointParam_vertexMain_clipIndex_0;


#line 10
layout(location = 0)
in vec2 input_unitPos_0;


#line 10
layout(location = 1)
in vec4 input_rect_0;


#line 10
layout(location = 2)
in vec4 input_atlasUV_0;


#line 10
layout(location = 3)
in uint input_color_0;


#line 10
layout(location = 4)
in uint input_clipIndex_0;

struct Varyings_0
{
    vec4 position_0;
    vec2 pixelPos_0;
    vec2 atlasUV_0;
    uint color_0;
    uint clipIndex_0;
};


#line 35
void main()
{

    vec2 pixelPos_1 = input_rect_0.xy + input_unitPos_0 * input_rect_0.zw;

#line 37
    Varyings_0 o_0;

    o_0.position_0 = (((vec4(pixelPos_1, 0.0, 1.0)) * (Globals_0.u_projection_0)));
    o_0.pixelPos_0 = pixelPos_1;
    o_0.atlasUV_0 = vec2(input_atlasUV_0.x + input_unitPos_0.x * input_atlasUV_0.z, input_atlasUV_0.y + input_unitPos_0.y * input_atlasUV_0.w);


    o_0.color_0 = input_color_0;
    o_0.clipIndex_0 = input_clipIndex_0;
    Varyings_0 _S1 = o_0;

#line 46
    gl_Position = o_0.position_0;

#line 46
    entryPointParam_vertexMain_pixelPos_0 = _S1.pixelPos_0;

#line 46
    entryPointParam_vertexMain_atlasUV_0 = _S1.atlasUV_0;

#line 46
    entryPointParam_vertexMain_color_0 = _S1.color_0;

#line 46
    entryPointParam_vertexMain_clipIndex_0 = _S1.clipIndex_0;

#line 46
    return;
}

vec4 unpackARGB_0(uint c_0)
{

#line 55
    return vec4(float((c_0 >> 16) & 255U) / 255.0, float((c_0 >> 8) & 255U) / 255.0, float(c_0 & 255U) / 255.0, float((c_0 >> 24) & 255U) / 255.0);
}


#line 55
layout(location = 0)
out vec4 entryPointParam_fragmentMain_0;


#line 55
layout(location = 0)
in vec2 v_pixelPos_0;


#line 55
layout(location = 1)
in vec2 v_atlasUV_0;


#line 55
flat layout(location = 2)
in uint v_color_0;


#line 55
flat layout(location = 3)
in uint v_clipIndex_0;


void main()
{
    vec4 clip_0 = ClipRects_0.u_clipRects_0[v_clipIndex_0];

#line 61
    bool _S2;
    if((v_pixelPos_0.x) < (ClipRects_0.u_clipRects_0[v_clipIndex_0].x))
    {

#line 62
        _S2 = true;

#line 62
    }
    else
    {

#line 62
        _S2 = (v_pixelPos_0.x) >= (clip_0.z);

#line 62
    }

#line 62
    if(_S2)
    {

#line 62
        _S2 = true;

#line 62
    }
    else
    {

#line 62
        _S2 = (v_pixelPos_0.y) < (clip_0.y);

#line 62
    }
    if(_S2)
    {

#line 63
        _S2 = true;

#line 63
    }
    else
    {

#line 63
        _S2 = (v_pixelPos_0.y) >= (clip_0.w);

#line 63
    }

#line 62
    if(_S2)
    {

        discard;

#line 62
    }

#line 68
    float alpha_0 = (texture((u_atlas_0), (v_atlasUV_0))).x;
    if(alpha_0 <= 0.0)
    {

#line 69
        discard;

#line 69
    }

    vec4 color_1 = unpackARGB_0(v_color_0);

#line 71
    entryPointParam_fragmentMain_0 = vec4(color_1.xyz, color_1.w * alpha_0);

#line 71
    return;
}

