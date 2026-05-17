#include <metal_stdlib>
#include <metal_math>
#include <metal_texture>
using namespace metal;

#line 49 "/Users/zee-seriesai/src/cs/ENV-Game-Framework/ZGF.Gui.Tests/Assets/Shaders/canvas_glyph.slang"
float4 unpackARGB_0(uint c_0)
{

#line 55
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


#line 22 "/Users/zee-seriesai/src/cs/ENV-Game-Framework/ZGF.Gui.Tests/Assets/Shaders/canvas_glyph.slang"
struct SLANG_ParameterGroup_Globals_0
{
    matrix<float,int(4),int(4)>  u_projection_0;
};

struct SLANG_ParameterGroup_ClipRects_0
{
    array<float4, int(256)> u_clipRects_0;
};


#line 27
struct KernelContext_0
{
    SLANG_ParameterGroup_Globals_0 constant* Globals_0;
    SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_0;
    texture2d<float, access::sample> u_atlas_texture_0;
    sampler u_atlas_sampler_0;
};


#line 59
[[fragment]] pixelOutput_0 fragmentMain(pixelInput_0 _S1 [[stage_in]], float4 position_0 [[position]], SLANG_ParameterGroup_Globals_0 constant* Globals_1 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_1 [[buffer(1)]], texture2d<float, access::sample> u_atlas_texture_1 [[texture(0)]], sampler u_atlas_sampler_1 [[sampler(0)]])
{

#line 59
    thread KernelContext_0 kernelContext_0;

#line 59
    (&kernelContext_0)->Globals_0 = Globals_1;

#line 59
    (&kernelContext_0)->ClipRects_0 = ClipRects_1;

#line 59
    (&kernelContext_0)->u_atlas_texture_0 = u_atlas_texture_1;

#line 59
    (&kernelContext_0)->u_atlas_sampler_0 = u_atlas_sampler_1;

    float4 clip_0 = ClipRects_1->u_clipRects_0[_S1.clipIndex_0];
    float _S2 = _S1.pixelPos_0.x;

#line 62
    bool _S3;

#line 62
    if(_S2 < (ClipRects_1->u_clipRects_0[_S1.clipIndex_0].x))
    {

#line 62
        _S3 = true;

#line 62
    }
    else
    {

#line 62
        _S3 = _S2 >= (clip_0.z);

#line 62
    }

#line 62
    if(_S3)
    {

#line 62
        _S3 = true;

#line 62
    }
    else
    {

#line 62
        _S3 = (_S1.pixelPos_0.y) < (clip_0.y);

#line 62
    }
    if(_S3)
    {

#line 63
        _S3 = true;

#line 63
    }
    else
    {

#line 63
        _S3 = (_S1.pixelPos_0.y) >= (clip_0.w);

#line 63
    }

#line 62
    if(_S3)
    {

        discard_fragment();

#line 62
    }

#line 68
    ;

#line 68
    float alpha_0 = (((&kernelContext_0)->u_atlas_texture_0).sample(((&kernelContext_0)->u_atlas_sampler_0), (_S1.atlasUV_0))).x;
    if(alpha_0 <= 0.0)
    {

#line 69
        discard_fragment();

#line 69
    }

    float4 color_1 = unpackARGB_0(_S1.color_0);

#line 71
    pixelOutput_0 _S4 = { float4(color_1.xyz, color_1.w * alpha_0) };
    return _S4;
}


#line 72
struct vertexMain_Result_0
{
    float4 position_1 [[position]];
    float2 pixelPos_1 [[user(TEXCOORD)]];
    float2 atlasUV_1 [[user(TEXCOORD_1)]];
    uint color_2 [[user(COLOR)]];
    uint clipIndex_1 [[user(COLOR_1)]];
};


#line 72
struct vertexInput_0
{
    float2 unitPos_0 [[attribute(0)]];
    float4 rect_0 [[attribute(1)]];
    float4 atlasUV_2 [[attribute(2)]];
    uint color_3 [[attribute(3)]];
    uint clipIndex_2 [[attribute(4)]];
};


#line 13
struct Varyings_0
{
    float4 position_2;
    float2 pixelPos_2;
    float2 atlasUV_3;
    [[flat]] uint color_4;
    [[flat]] uint clipIndex_3;
};


#line 13
[[vertex]] vertexMain_Result_0 vertexMain(vertexInput_0 _S5 [[stage_in]], SLANG_ParameterGroup_Globals_0 constant* Globals_2 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_2 [[buffer(1)]], texture2d<float, access::sample> u_atlas_texture_2 [[texture(0)]], sampler u_atlas_sampler_2 [[sampler(0)]])
{

#line 13
    thread KernelContext_0 kernelContext_1;

#line 13
    (&kernelContext_1)->Globals_0 = Globals_2;

#line 13
    (&kernelContext_1)->ClipRects_0 = ClipRects_2;

#line 13
    (&kernelContext_1)->u_atlas_texture_0 = u_atlas_texture_2;

#line 13
    (&kernelContext_1)->u_atlas_sampler_0 = u_atlas_sampler_2;

#line 38
    float2 pixelPos_3 = _S5.rect_0.xy + _S5.unitPos_0 * _S5.rect_0.zw;

#line 37
    thread Varyings_0 o_0;

    (&o_0)->position_2 = (((float4(pixelPos_3, 0.0, 1.0)) * (Globals_2->u_projection_0)));
    (&o_0)->pixelPos_2 = pixelPos_3;
    (&o_0)->atlasUV_3 = float2(_S5.atlasUV_2.x + _S5.unitPos_0.x * _S5.atlasUV_2.z, _S5.atlasUV_2.y + _S5.unitPos_0.y * _S5.atlasUV_2.w);


    (&o_0)->color_4 = _S5.color_3;
    (&o_0)->clipIndex_3 = _S5.clipIndex_2;

#line 45
    thread vertexMain_Result_0 _S6;

#line 45
    (&_S6)->position_1 = o_0.position_2;

#line 45
    (&_S6)->pixelPos_1 = o_0.pixelPos_2;

#line 45
    (&_S6)->atlasUV_1 = o_0.atlasUV_3;

#line 45
    (&_S6)->color_2 = o_0.color_4;

#line 45
    (&_S6)->clipIndex_1 = o_0.clipIndex_3;

#line 45
    return _S6;
}

