#include <metal_stdlib>
#include <metal_math>
#include <metal_texture>
using namespace metal;

#line 55 "G:/Dev/RiderProjects/ENV-Game-Framework/ZGF.Gui.Tests/Assets/Shaders/canvas_glyph.slang"
float4 unpackARGB_0(uint c_0)
{

#line 61
    return float4(float((c_0 >> 16U) & 255U) / 255.0, float((c_0 >> 8U) & 255U) / 255.0, float(c_0 & 255U) / 255.0, float((c_0 >> 24U) & 255U) / 255.0);
}


#line 90 "core"
struct pixelOutput_0
{
    float4 output_0 [[color(0)]];
};


#line 90
struct pixelInput_0
{
    float2 pixelPos_0 [[user(TEXCOORD)]];
    float2 atlasUV_0 [[user(TEXCOORD_1)]];
    [[flat]] uint color_0 [[user(COLOR)]];
    [[flat]] uint clipIndex_0 [[user(COLOR_1)]];
};


#line 23 "G:/Dev/RiderProjects/ENV-Game-Framework/ZGF.Gui.Tests/Assets/Shaders/canvas_glyph.slang"
struct SLANG_ParameterGroup_Globals_0
{
    matrix<float,int(4),int(4)>  u_projection_0;
};

struct SLANG_ParameterGroup_ClipRects_0
{
    array<float4, int(256)> u_clipRects_0;
};


#line 28
struct KernelContext_0
{
    SLANG_ParameterGroup_Globals_0 constant* Globals_0;
    SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_0;
    texture2d<float, access::sample> u_atlas_texture_0;
    sampler u_atlas_sampler_0;
};


#line 65
[[fragment]] pixelOutput_0 fragmentMain(pixelInput_0 _S1 [[stage_in]], float4 position_0 [[position]], SLANG_ParameterGroup_Globals_0 constant* Globals_1 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_1 [[buffer(1)]], texture2d<float, access::sample> u_atlas_texture_1 [[texture(0)]], sampler u_atlas_sampler_1 [[sampler(0)]])
{

#line 65
    thread KernelContext_0 kernelContext_0;

#line 65
    (&kernelContext_0)->Globals_0 = Globals_1;

#line 65
    (&kernelContext_0)->ClipRects_0 = ClipRects_1;

#line 65
    (&kernelContext_0)->u_atlas_texture_0 = u_atlas_texture_1;

#line 65
    (&kernelContext_0)->u_atlas_sampler_0 = u_atlas_sampler_1;

    float4 clip_0 = ClipRects_1->u_clipRects_0[_S1.clipIndex_0];
    float _S2 = _S1.pixelPos_0.x;

#line 68
    bool _S3;

#line 68
    if(_S2 < (ClipRects_1->u_clipRects_0[_S1.clipIndex_0].x))
    {

#line 68
        _S3 = true;

#line 68
    }
    else
    {

#line 68
        _S3 = _S2 >= (clip_0.z);

#line 68
    }

#line 68
    if(_S3)
    {

#line 68
        _S3 = true;

#line 68
    }
    else
    {

#line 68
        _S3 = (_S1.pixelPos_0.y) < (clip_0.y);

#line 68
    }
    if(_S3)
    {

#line 69
        _S3 = true;

#line 69
    }
    else
    {

#line 69
        _S3 = (_S1.pixelPos_0.y) >= (clip_0.w);

#line 69
    }

#line 68
    if(_S3)
    {

        discard_fragment();

#line 68
    }

#line 74
    ;

#line 74
    float alpha_0 = (((&kernelContext_0)->u_atlas_texture_0).sample(((&kernelContext_0)->u_atlas_sampler_0), (_S1.atlasUV_0))).x;
    if(alpha_0 <= 0.0)
    {

#line 75
        discard_fragment();

#line 75
    }

    float4 color_1 = unpackARGB_0(_S1.color_0);

#line 77
    pixelOutput_0 _S4 = { float4(color_1.xyz, color_1.w * alpha_0) };
    return _S4;
}


#line 78
struct vertexMain_Result_0
{
    float4 position_1 [[position]];
    float2 pixelPos_1 [[user(TEXCOORD)]];
    float2 atlasUV_1 [[user(TEXCOORD_1)]];
    uint color_2 [[user(COLOR)]];
    uint clipIndex_1 [[user(COLOR_1)]];
};


#line 78
struct vertexInput_0
{
    float2 unitPos_0 [[attribute(0)]];
    float4 rect_0 [[attribute(1)]];
    float4 atlasUV_2 [[attribute(2)]];
    uint color_3 [[attribute(3)]];
    uint clipIndex_2 [[attribute(4)]];
    float rotation_0 [[attribute(5)]];
};


#line 14
struct Varyings_0
{
    float4 position_2;
    float2 pixelPos_2;
    float2 atlasUV_3;
    [[flat]] uint color_4;
    [[flat]] uint clipIndex_3;
};


#line 14
[[vertex]] vertexMain_Result_0 vertexMain(vertexInput_0 _S5 [[stage_in]], SLANG_ParameterGroup_Globals_0 constant* Globals_2 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_2 [[buffer(1)]], texture2d<float, access::sample> u_atlas_texture_2 [[texture(0)]], sampler u_atlas_sampler_2 [[sampler(0)]])
{

#line 14
    thread KernelContext_0 kernelContext_1;

#line 14
    (&kernelContext_1)->Globals_0 = Globals_2;

#line 14
    (&kernelContext_1)->ClipRects_0 = ClipRects_2;

#line 14
    (&kernelContext_1)->u_atlas_texture_0 = u_atlas_texture_2;

#line 14
    (&kernelContext_1)->u_atlas_sampler_0 = u_atlas_sampler_2;

#line 39
    float2 _S6 = _S5.rect_0.zw;
    float2 local_0 = (_S5.unitPos_0 - float2(0.5, 0.5)) * _S6;
    float cs_0 = cos(_S5.rotation_0);
    float sn_0 = sin(_S5.rotation_0);
    float _S7 = local_0.x;

#line 43
    float _S8 = local_0.y;
    float2 pixelPos_3 = _S5.rect_0.xy + _S6 * float2(0.5)  + float2(cs_0 * _S7 - sn_0 * _S8, sn_0 * _S7 + cs_0 * _S8);

#line 38
    thread Varyings_0 o_0;

#line 45
    (&o_0)->position_2 = (((float4(pixelPos_3, 0.0, 1.0)) * (Globals_2->u_projection_0)));
    (&o_0)->pixelPos_2 = pixelPos_3;
    (&o_0)->atlasUV_3 = float2(_S5.atlasUV_2.x + _S5.unitPos_0.x * _S5.atlasUV_2.z, _S5.atlasUV_2.y + _S5.unitPos_0.y * _S5.atlasUV_2.w);


    (&o_0)->color_4 = _S5.color_3;
    (&o_0)->clipIndex_3 = _S5.clipIndex_2;

#line 51
    thread vertexMain_Result_0 _S9;

#line 51
    (&_S9)->position_1 = o_0.position_2;

#line 51
    (&_S9)->pixelPos_1 = o_0.pixelPos_2;

#line 51
    (&_S9)->atlasUV_1 = o_0.atlasUV_3;

#line 51
    (&_S9)->color_2 = o_0.color_4;

#line 51
    (&_S9)->clipIndex_1 = o_0.clipIndex_3;

#line 51
    return _S9;
}

