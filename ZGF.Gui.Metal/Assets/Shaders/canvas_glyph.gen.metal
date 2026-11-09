// Generated from ZGF.Gui.Shaders/canvas_glyph.slang by ZGF.Gui.Shaders/regenerate.cs. Edit the .slang and regenerate.
#include <metal_stdlib>
#include <metal_math>
#include <metal_texture>
using namespace metal;

#line 11 "canvas_common.slangh"
struct _MatrixStorage_float4x4_ColMajornatural_0
{
    array<float4, int(4)> data_0;
};


#line 11
struct GlobalsData_natural_0
{
    _MatrixStorage_float4x4_ColMajornatural_0 projection_0;
    float4 canvasMetrics_0;
};


#line 13
struct ClipRectsData_0
{
    array<float4, int(256)> rects_0;
    array<float4, int(256)> rounds_0;
    array<float4, int(256)> radii_0;
};


#line 3 "canvas_glyph.slang"
struct KernelContext_0
{
    GlobalsData_natural_0 constant* Globals_0;
    ClipRectsData_0 constant* ClipRects_0;
    texture2d<float, access::sample> u_atlas_texture_0;
    sampler u_atlas_sampler_0;
};


#line 22 "canvas_common.slangh"
float4 project_0(float2 pixelPos_0, KernelContext_0 thread* kernelContext_0)
{
    return (((float4(pixelPos_0, 0.0f, 1.0f)) * (matrix<float,int(4),int(4)> (kernelContext_0->Globals_0->projection_0.data_0[int(0)][int(0)], kernelContext_0->Globals_0->projection_0.data_0[int(1)][int(0)], kernelContext_0->Globals_0->projection_0.data_0[int(2)][int(0)], kernelContext_0->Globals_0->projection_0.data_0[int(3)][int(0)], kernelContext_0->Globals_0->projection_0.data_0[int(0)][int(1)], kernelContext_0->Globals_0->projection_0.data_0[int(1)][int(1)], kernelContext_0->Globals_0->projection_0.data_0[int(2)][int(1)], kernelContext_0->Globals_0->projection_0.data_0[int(3)][int(1)], kernelContext_0->Globals_0->projection_0.data_0[int(0)][int(2)], kernelContext_0->Globals_0->projection_0.data_0[int(1)][int(2)], kernelContext_0->Globals_0->projection_0.data_0[int(2)][int(2)], kernelContext_0->Globals_0->projection_0.data_0[int(3)][int(2)], kernelContext_0->Globals_0->projection_0.data_0[int(0)][int(3)], kernelContext_0->Globals_0->projection_0.data_0[int(1)][int(3)], kernelContext_0->Globals_0->projection_0.data_0[int(2)][int(3)], kernelContext_0->Globals_0->projection_0.data_0[int(3)][int(3)]))));
}


#line 50
float clipCoverage_0(float2 p_0, uint index_0, KernelContext_0 thread* kernelContext_1)
{
    float _S1 = max((fwidth((p_0.x))), 9.9999997473787516e-05f);
    float r_0 = kernelContext_1->ClipRects_0->radii_0[index_0].x;
    if(r_0 <= 0.0f)
    {

#line 54
        return 1.0f;
    }
    float2 _S2 = kernelContext_1->ClipRects_0->rounds_0[index_0].zw;

#line 56
    float2 _S3 = kernelContext_1->ClipRects_0->rounds_0[index_0].xy;

#line 56
    float2 _S4 = float2(0.5f) ;
    float2 q_0 = abs(p_0 - (_S3 + _S2) * _S4) - ((_S2 - _S3) * _S4 - float2(r_0) );

    return clamp(0.5f - (length(max(q_0, float2(0.0f) )) + min(max(q_0.x, q_0.y), 0.0f) - r_0) / _S1, 0.0f, 1.0f);
}


bool outsideClipRect_0(float2 p_1, uint index_1, KernelContext_0 thread* kernelContext_2)
{
    float4 clip_0 = kernelContext_2->ClipRects_0->rects_0[index_1];
    float _S5 = p_1.x;

#line 66
    bool _S6;

#line 66
    if(_S5 < (kernelContext_2->ClipRects_0->rects_0[index_1].x))
    {

#line 66
        _S6 = true;

#line 66
    }
    else
    {

#line 66
        _S6 = _S5 >= (clip_0.z);

#line 66
    }

#line 66
    if(_S6)
    {

#line 66
        _S6 = true;

#line 66
    }
    else
    {

#line 66
        _S6 = (p_1.y) < (clip_0.y);

#line 66
    }

#line 66
    if(_S6)
    {

#line 66
        _S6 = true;

#line 66
    }
    else
    {

#line 66
        _S6 = (p_1.y) >= (clip_0.w);

#line 66
    }

#line 66
    return _S6;
}


#line 39
float4 unpackARGB_0(uint c_0)
{

#line 45
    return float4(float((c_0 >> 16U) & 255U) / 255.0f, float((c_0 >> 8U) & 255U) / 255.0f, float(c_0 & 255U) / 255.0f, float((c_0 >> 24U) & 255U) / 255.0f);
}


#line 45
struct pixelOutput_0
{
    float4 output_0 [[color(0)]];
};


#line 45
struct pixelInput_0
{
    float2 pixelPos_1 [[user(PIXEL_POS)]];
    float2 atlasUV_0 [[user(ATLAS_UV)]];
    [[flat]] uint color_0 [[user(COLOR)]];
    [[flat]] uint clipIndex_0 [[user(CLIP_INDEX)]];
};


#line 48 "canvas_glyph.slang"
[[fragment]] pixelOutput_0 fragmentMain(pixelInput_0 _S7 [[stage_in]], float4 position_0 [[position]], GlobalsData_natural_0 constant* Globals_1 [[buffer(0)]], ClipRectsData_0 constant* ClipRects_1 [[buffer(1)]], texture2d<float, access::sample> u_atlas_texture_1 [[texture(0)]], sampler u_atlas_sampler_1 [[sampler(0)]])
{

#line 48
    thread KernelContext_0 kernelContext_3;

#line 48
    (&kernelContext_3)->Globals_0 = Globals_1;

#line 48
    (&kernelContext_3)->ClipRects_0 = ClipRects_1;

#line 48
    (&kernelContext_3)->u_atlas_texture_0 = u_atlas_texture_1;

#line 48
    (&kernelContext_3)->u_atlas_sampler_0 = u_atlas_sampler_1;

#line 48
    float _S8 = clipCoverage_0(_S7.pixelPos_1, _S7.clipIndex_0, &kernelContext_3);

#line 48
    bool _S9 = outsideClipRect_0(_S7.pixelPos_1, _S7.clipIndex_0, &kernelContext_3);


    if(_S9)
    {

#line 51
        discard_fragment();

#line 51
    }
    if(_S8 <= 0.0f)
    {

#line 52
        discard_fragment();

#line 52
    }

    ;

#line 54
    float alpha_0 = (((&kernelContext_3)->u_atlas_texture_0).sample(((&kernelContext_3)->u_atlas_sampler_0), (_S7.atlasUV_0))).x;
    if(alpha_0 <= 0.0f)
    {

#line 55
        discard_fragment();

#line 55
    }

    float4 color_1 = unpackARGB_0(_S7.color_0);

#line 57
    pixelOutput_0 _S10 = { float4(color_1.xyz, color_1.w * alpha_0 * _S8) };
    return _S10;
}


#line 58
struct vertexMain_Result_0
{
    float4 position_1 [[position]];
    float2 pixelPos_2 [[user(PIXEL_POS)]];
    float2 atlasUV_1 [[user(ATLAS_UV)]];
    uint color_2 [[user(COLOR)]];
    uint clipIndex_1 [[user(CLIP_INDEX)]];
};


#line 58
struct vertexInput_0
{
    float2 unitPos_0 [[attribute(0)]];
    float4 rect_0 [[attribute(1)]];
    float4 atlasUV_2 [[attribute(2)]];
    uint color_3 [[attribute(3)]];
    uint clipIndex_2 [[attribute(4)]];
    float rotation_0 [[attribute(5)]];
};


#line 15
struct Varyings_0
{
    float4 position_2;
    float2 pixelPos_3;
    float2 atlasUV_3;
    [[flat]] uint color_4;
    [[flat]] uint clipIndex_3;
};


#line 15
[[vertex]] vertexMain_Result_0 vertexMain(vertexInput_0 _S11 [[stage_in]], GlobalsData_natural_0 constant* Globals_2 [[buffer(0)]], ClipRectsData_0 constant* ClipRects_2 [[buffer(1)]], texture2d<float, access::sample> u_atlas_texture_2 [[texture(0)]], sampler u_atlas_sampler_2 [[sampler(0)]])
{

#line 15
    thread KernelContext_0 kernelContext_4;

#line 15
    (&kernelContext_4)->Globals_0 = Globals_2;

#line 15
    (&kernelContext_4)->ClipRects_0 = ClipRects_2;

#line 15
    (&kernelContext_4)->u_atlas_texture_0 = u_atlas_texture_2;

#line 15
    (&kernelContext_4)->u_atlas_sampler_0 = u_atlas_sampler_2;

#line 27
    float2 _S12 = _S11.rect_0.zw;
    float2 local_0 = (_S11.unitPos_0 - float2(0.5f) ) * _S12;
    float cs_0 = cos(_S11.rotation_0);
    float sn_0 = sin(_S11.rotation_0);
    float _S13 = local_0.x;

#line 31
    float _S14 = local_0.y;
    float2 pixelPos_4 = _S11.rect_0.xy + _S12 * float2(0.5f)  + float2(cs_0 * _S13 - sn_0 * _S14, sn_0 * _S13 + cs_0 * _S14);

    thread Varyings_0 o_0;

#line 34
    float4 _S15 = project_0(pixelPos_4, &kernelContext_4);
    (&o_0)->position_2 = _S15;
    (&o_0)->pixelPos_3 = pixelPos_4;


    (&o_0)->atlasUV_3 = float2(_S11.atlasUV_2.x + _S11.unitPos_0.x * _S11.atlasUV_2.z, _S11.atlasUV_2.y + _S11.unitPos_0.y * _S11.atlasUV_2.w);


    (&o_0)->color_4 = _S11.color_3;
    (&o_0)->clipIndex_3 = _S11.clipIndex_2;

#line 43
    thread vertexMain_Result_0 _S16;

#line 43
    (&_S16)->position_1 = o_0.position_2;

#line 43
    (&_S16)->pixelPos_2 = o_0.pixelPos_3;

#line 43
    (&_S16)->atlasUV_1 = o_0.atlasUV_3;

#line 43
    (&_S16)->color_2 = o_0.color_4;

#line 43
    (&_S16)->clipIndex_1 = o_0.clipIndex_3;

#line 43
    return _S16;
}

