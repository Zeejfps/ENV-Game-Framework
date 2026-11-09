// Generated from ZGF.Gui.Shaders/canvas_image.slang by ZGF.Gui.Shaders/regenerate.cs. Edit the .slang and regenerate.
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


#line 3 "canvas_image.slang"
struct KernelContext_0
{
    GlobalsData_natural_0 constant* Globals_0;
    ClipRectsData_0 constant* ClipRects_0;
    texture2d<float, access::sample> u_texture_texture_0;
    sampler u_texture_sampler_0;
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
    float2 uv_0 [[user(UV)]];
    float2 local_0 [[user(LOCAL)]];
    [[flat]] float2 halfSize_0 [[user(HALF_SIZE)]];
    [[flat]] float4 srcUV_0 [[user(SRC_UV)]];
    [[flat]] float fadesEdges_0 [[user(FADES_EDGES)]];
    [[flat]] uint tint_0 [[user(TINT)]];
    [[flat]] uint clipIndex_0 [[user(CLIP_INDEX)]];
    [[flat]] float4 cornerRadius_0 [[user(CORNER_RADIUS)]];
};


#line 72 "canvas_image.slang"
[[fragment]] pixelOutput_0 fragmentMain(pixelInput_0 _S7 [[stage_in]], float4 position_0 [[position]], GlobalsData_natural_0 constant* Globals_1 [[buffer(0)]], ClipRectsData_0 constant* ClipRects_1 [[buffer(1)]], texture2d<float, access::sample> u_texture_texture_1 [[texture(0)]], sampler u_texture_sampler_1 [[sampler(0)]])
{

#line 72
    thread KernelContext_0 kernelContext_3;

#line 72
    (&kernelContext_3)->Globals_0 = Globals_1;

#line 72
    (&kernelContext_3)->ClipRects_0 = ClipRects_1;

#line 72
    (&kernelContext_3)->u_texture_texture_0 = u_texture_texture_1;

#line 72
    (&kernelContext_3)->u_texture_sampler_0 = u_texture_sampler_1;

#line 72
    float _S8 = clipCoverage_0(_S7.pixelPos_1, _S7.clipIndex_0, &kernelContext_3);

#line 72
    bool _S9 = outsideClipRect_0(_S7.pixelPos_1, _S7.clipIndex_0, &kernelContext_3);


    if(_S9)
    {

#line 75
        discard_fragment();

#line 75
    }
    if(_S8 <= 0.0f)
    {

#line 76
        discard_fragment();

#line 76
    }


    float2 devicePerLogical_0 = (&kernelContext_3)->Globals_0->canvasMetrics_0.zw / (&kernelContext_3)->Globals_0->canvasMetrics_0.xy;

#line 79
    float coverage_0;



    if((_S7.fadesEdges_0) > 0.0f)
    {
        float2 inside_0 = (_S7.halfSize_0 - abs(_S7.local_0)) * devicePerLogical_0;
        float coverage_1 = clamp(inside_0.x + 0.5f, 0.0f, 1.0f) * clamp(inside_0.y + 0.5f, 0.0f, 1.0f);
        if(coverage_1 <= 0.0f)
        {

#line 87
            discard_fragment();

#line 87
        }

#line 87
        coverage_0 = coverage_1;

#line 83
    }
    else
    {

#line 83
        coverage_0 = 1.0f;

#line 83
    }

#line 83
    float radius_0;

#line 93
    if((_S7.local_0.y) > 0.0f)
    {

#line 93
        if((_S7.local_0.x) > 0.0f)
        {

#line 93
            radius_0 = _S7.cornerRadius_0.y;

#line 93
        }
        else
        {

#line 93
            radius_0 = _S7.cornerRadius_0.x;

#line 93
        }

#line 93
    }
    else
    {

#line 94
        if((_S7.local_0.x) > 0.0f)
        {

#line 94
            radius_0 = _S7.cornerRadius_0.z;

#line 94
        }
        else
        {

#line 94
            radius_0 = _S7.cornerRadius_0.w;

#line 94
        }

#line 93
    }

    if(radius_0 > 0.0f)
    {
        float2 q_1 = abs(_S7.local_0) - (_S7.halfSize_0 - float2(radius_0) );

        float coverage_2 = coverage_0 * clamp(0.5f - (length(max(q_1, float2(0.0f) )) + min(max(q_1.x, q_1.y), 0.0f) - radius_0) * devicePerLogical_0.x, 0.0f, 1.0f);
        if(coverage_2 <= 0.0f)
        {

#line 100
            discard_fragment();

#line 100
        }

#line 100
        coverage_0 = coverage_2;

#line 95
    }

#line 104
    float2 _S10 = _S7.srcUV_0.xy;

#line 104
    float2 uvEnd_0 = _S10 + _S7.srcUV_0.zw;
    float2 uv_1 = clamp(_S7.uv_0, min(_S10, uvEnd_0), max(_S10, uvEnd_0));
    ;

#line 106
    pixelOutput_0 _S11 = { (((&kernelContext_3)->u_texture_texture_0).sample(((&kernelContext_3)->u_texture_sampler_0), (uv_1))) * unpackARGB_0(_S7.tint_0) * float4(1.0f, 1.0f, 1.0f, coverage_0 * _S8) };

    return _S11;
}


#line 108
struct vertexMain_Result_0
{
    float4 position_1 [[position]];
    float2 pixelPos_2 [[user(PIXEL_POS)]];
    float2 uv_2 [[user(UV)]];
    float2 local_1 [[user(LOCAL)]];
    float2 halfSize_1 [[user(HALF_SIZE)]];
    float4 srcUV_1 [[user(SRC_UV)]];
    float fadesEdges_1 [[user(FADES_EDGES)]];
    uint tint_1 [[user(TINT)]];
    uint clipIndex_1 [[user(CLIP_INDEX)]];
    float4 cornerRadius_1 [[user(CORNER_RADIUS)]];
};


#line 108
struct vertexInput_0
{
    float2 unitPos_0 [[attribute(0)]];
    float4 rect_0 [[attribute(1)]];
    float4 srcUV_2 [[attribute(2)]];
    uint tint_2 [[attribute(3)]];
    uint clipIndex_2 [[attribute(4)]];
    float rotation_0 [[attribute(5)]];
    float4 cornerRadius_2 [[attribute(6)]];
};


#line 16
struct Varyings_0
{
    float4 position_2;
    float2 pixelPos_3;
    float2 uv_3;
    float2 local_2;
    [[flat]] float2 halfSize_2;
    [[flat]] float4 srcUV_3;
    [[flat]] float fadesEdges_2;
    [[flat]] uint tint_3;
    [[flat]] uint clipIndex_3;
    [[flat]] float4 cornerRadius_3;
};


#line 16
[[vertex]] vertexMain_Result_0 vertexMain(vertexInput_0 _S12 [[stage_in]], GlobalsData_natural_0 constant* Globals_2 [[buffer(0)]], ClipRectsData_0 constant* ClipRects_2 [[buffer(1)]], texture2d<float, access::sample> u_texture_texture_2 [[texture(0)]], sampler u_texture_sampler_2 [[sampler(0)]])
{

#line 16
    thread KernelContext_0 kernelContext_4;

#line 16
    (&kernelContext_4)->Globals_0 = Globals_2;

#line 16
    (&kernelContext_4)->ClipRects_0 = ClipRects_2;

#line 16
    (&kernelContext_4)->u_texture_texture_0 = u_texture_texture_2;

#line 16
    (&kernelContext_4)->u_texture_sampler_0 = u_texture_sampler_2;

#line 39
    float2 devicePerLogical_1 = Globals_2->canvasMetrics_0.zw / Globals_2->canvasMetrics_0.xy;
    float2 _S13 = _S12.rect_0.xy;

#line 40
    float2 low_0 = _S13 * devicePerLogical_1;
    float2 _S14 = _S12.rect_0.zw;

#line 41
    float2 high_0 = (_S13 + _S14) * devicePerLogical_1;
    float2 _S15 = max(abs(low_0 - round(low_0)), abs(high_0 - round(high_0)));

#line 42
    bool growable_0;
    if((_S12.rotation_0) == 0.0f)
    {

#line 43
        growable_0 = (_S12.rect_0.z) > 0.0f;

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
        growable_0 = (_S12.rect_0.w) > 0.0f;

#line 43
    }
    else
    {

#line 43
        growable_0 = false;

#line 43
    }

#line 43
    float2 outset_0;
    if(growable_0)
    {

#line 44
        outset_0 = step(float2(0.00390625f) , _S15) / devicePerLogical_1;

#line 44
    }
    else
    {

#line 44
        outset_0 = float2(0.0f) ;

#line 44
    }
    float2 unit_0 = (_S12.unitPos_0 * (_S14 + float2(2.0f)  * outset_0) - outset_0) / max(_S14, float2(9.99999997475242708e-07f) );

    float2 _S16 = _S14 * float2(0.5f) ;
    float2 local_3 = (unit_0 - float2(0.5f) ) * _S14;
    float cs_0 = cos(_S12.rotation_0);
    float sn_0 = sin(_S12.rotation_0);
    float _S17 = local_3.x;

#line 51
    float _S18 = local_3.y;
    float2 pixelPos_4 = _S13 + _S16 + float2(cs_0 * _S17 - sn_0 * _S18, sn_0 * _S17 + cs_0 * _S18);

    thread Varyings_0 o_0;

#line 54
    float4 _S19 = project_0(pixelPos_4, &kernelContext_4);
    (&o_0)->position_2 = _S19;
    (&o_0)->pixelPos_3 = pixelPos_4;

    (&o_0)->uv_3 = float2(_S12.srcUV_2.x + unit_0.x * _S12.srcUV_2.z, _S12.srcUV_2.y + unit_0.y * _S12.srcUV_2.w);


    (&o_0)->local_2 = local_3;
    (&o_0)->halfSize_2 = _S16;
    (&o_0)->srcUV_3 = _S12.srcUV_2;
    if((outset_0.x) > 0.0f)
    {

#line 64
        growable_0 = true;

#line 64
    }
    else
    {

#line 64
        growable_0 = (outset_0.y) > 0.0f;

#line 64
    }

#line 64
    float _S20;

#line 64
    if(growable_0)
    {

#line 64
        _S20 = 1.0f;

#line 64
    }
    else
    {

#line 64
        _S20 = 0.0f;

#line 64
    }

#line 64
    (&o_0)->fadesEdges_2 = _S20;
    (&o_0)->tint_3 = _S12.tint_2;
    (&o_0)->clipIndex_3 = _S12.clipIndex_2;
    (&o_0)->cornerRadius_3 = min(_S12.cornerRadius_2, float4((min(_S12.rect_0.z, _S12.rect_0.w) * 0.5f)) );
    Varyings_0 _S21 = o_0;

#line 68
    thread vertexMain_Result_0 _S22;

#line 68
    (&_S22)->position_1 = _S21.position_2;

#line 68
    (&_S22)->pixelPos_2 = _S21.pixelPos_3;

#line 68
    (&_S22)->uv_2 = _S21.uv_3;

#line 68
    (&_S22)->local_1 = _S21.local_2;

#line 68
    (&_S22)->halfSize_1 = _S21.halfSize_2;

#line 68
    (&_S22)->srcUV_1 = _S21.srcUV_3;

#line 68
    (&_S22)->fadesEdges_1 = _S21.fadesEdges_2;

#line 68
    (&_S22)->tint_1 = _S21.tint_3;

#line 68
    (&_S22)->clipIndex_1 = _S21.clipIndex_3;

#line 68
    (&_S22)->cornerRadius_1 = _S21.cornerRadius_3;

#line 68
    return _S22;
}

