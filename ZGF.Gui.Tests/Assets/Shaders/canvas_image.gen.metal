#include <metal_stdlib>
#include <metal_math>
#include <metal_texture>
using namespace metal;

#line 49 "/Users/zee-seriesai/src/cs/ENV-Game-Framework/.claude/worktrees/metal-canvas-backend/ZGF.Gui.Tests/Assets/Shaders/canvas_image.slang"
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
    float2 uv_0 [[user(TEXCOORD_1)]];
    [[flat]] uint tint_0 [[user(COLOR)]];
    [[flat]] uint clipIndex_0 [[user(COLOR_1)]];
};


#line 22 "/Users/zee-seriesai/src/cs/ENV-Game-Framework/.claude/worktrees/metal-canvas-backend/ZGF.Gui.Tests/Assets/Shaders/canvas_image.slang"
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
    texture2d<float, access::sample> u_texture_texture_0;
    sampler u_texture_sampler_0;
};


#line 59
[[fragment]] pixelOutput_0 fragmentMain(pixelInput_0 _S1 [[stage_in]], float4 position_0 [[position]], SLANG_ParameterGroup_Globals_0 constant* Globals_1 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_1 [[buffer(1)]], texture2d<float, access::sample> u_texture_texture_1 [[texture(0)]], sampler u_texture_sampler_1 [[sampler(0)]])
{

#line 59
    thread KernelContext_0 kernelContext_0;

#line 59
    (&kernelContext_0)->Globals_0 = Globals_1;

#line 59
    (&kernelContext_0)->ClipRects_0 = ClipRects_1;

#line 59
    (&kernelContext_0)->u_texture_texture_0 = u_texture_texture_1;

#line 59
    (&kernelContext_0)->u_texture_sampler_0 = u_texture_sampler_1;

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
    pixelOutput_0 _S4 = { (((&kernelContext_0)->u_texture_texture_0).sample(((&kernelContext_0)->u_texture_sampler_0), (_S1.uv_0))) * unpackARGB_0(_S1.tint_0) };

    return _S4;
}


#line 70
struct vertexMain_Result_0
{
    float4 position_1 [[position]];
    float2 pixelPos_1 [[user(TEXCOORD)]];
    float2 uv_1 [[user(TEXCOORD_1)]];
    uint tint_1 [[user(COLOR)]];
    uint clipIndex_1 [[user(COLOR_1)]];
};


#line 70
struct vertexInput_0
{
    float2 unitPos_0 [[attribute(0)]];
    float4 rect_0 [[attribute(1)]];
    float4 srcUV_0 [[attribute(2)]];
    uint tint_2 [[attribute(3)]];
    uint clipIndex_2 [[attribute(4)]];
};


#line 13
struct Varyings_0
{
    float4 position_2;
    float2 pixelPos_2;
    float2 uv_2;
    [[flat]] uint tint_3;
    [[flat]] uint clipIndex_3;
};


#line 13
[[vertex]] vertexMain_Result_0 vertexMain(vertexInput_0 _S5 [[stage_in]], SLANG_ParameterGroup_Globals_0 constant* Globals_2 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_2 [[buffer(1)]], texture2d<float, access::sample> u_texture_texture_2 [[texture(0)]], sampler u_texture_sampler_2 [[sampler(0)]])
{

#line 13
    thread KernelContext_0 kernelContext_1;

#line 13
    (&kernelContext_1)->Globals_0 = Globals_2;

#line 13
    (&kernelContext_1)->ClipRects_0 = ClipRects_2;

#line 13
    (&kernelContext_1)->u_texture_texture_0 = u_texture_texture_2;

#line 13
    (&kernelContext_1)->u_texture_sampler_0 = u_texture_sampler_2;

#line 38
    float2 pixelPos_3 = _S5.rect_0.xy + _S5.unitPos_0 * _S5.rect_0.zw;

#line 37
    thread Varyings_0 o_0;

    (&o_0)->position_2 = (((float4(pixelPos_3, 0.0, 1.0)) * (Globals_2->u_projection_0)));
    (&o_0)->pixelPos_2 = pixelPos_3;
    (&o_0)->uv_2 = float2(_S5.srcUV_0.x + _S5.unitPos_0.x * _S5.srcUV_0.z, _S5.srcUV_0.y + _S5.unitPos_0.y * _S5.srcUV_0.w);


    (&o_0)->tint_3 = _S5.tint_2;
    (&o_0)->clipIndex_3 = _S5.clipIndex_2;

#line 45
    thread vertexMain_Result_0 _S6;

#line 45
    (&_S6)->position_1 = o_0.position_2;

#line 45
    (&_S6)->pixelPos_1 = o_0.pixelPos_2;

#line 45
    (&_S6)->uv_1 = o_0.uv_2;

#line 45
    (&_S6)->tint_1 = o_0.tint_3;

#line 45
    (&_S6)->clipIndex_1 = o_0.clipIndex_3;

#line 45
    return _S6;
}

