// Generated from ZGF.Gui.Shaders/canvas_shadow.slang by ZGF.Gui.Shaders/regenerate.cs. Edit the .slang and regenerate.
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


#line 13
struct KernelContext_0
{
    GlobalsData_natural_0 constant* Globals_0;
    ClipRectsData_0 constant* ClipRects_0;
};




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


#line 45 "canvas_shadow.slang"
float2 erf2_0(float2 x_0)
{
    float2 _S7 = float2((vec<int,2>(sign((x_0)))));
    float2 a_0 = abs(x_0);
    float2 v_0 = float2(1.0f)  + (float2(0.27839300036430359f)  + (float2(0.23038899898529053f)  + float2(0.07810799777507782f)  * (a_0 * a_0)) * a_0) * a_0;
    float2 v_1 = v_0 * v_0;
    return _S7 - _S7 / (v_1 * v_1);
}


#line 60
float roundedBoxShadowX_0(float x_1, float y_0, float sigma_0, float corner_0, float2 halfSize_0)
{
    float _S8 = min(halfSize_0.y - corner_0 - abs(y_0), 0.0f);
    float curved_0 = halfSize_0.x - corner_0 + sqrt(max(0.0f, corner_0 * corner_0 - _S8 * _S8));

#line 63
    float2 _S9 = float2(0.5f) ;
    float2 integral_0 = _S9 + _S9 * erf2_0(float2(x_1 - curved_0, x_1 + curved_0) * float2((sqrt(0.5f) / sigma_0)) );
    return integral_0.y - integral_0.x;
}


#line 54
float gaussian1D_0(float x_2, float sigma_1)
{

    return exp(- (x_2 * x_2) / (2.0f * sigma_1 * sigma_1)) / (sqrt(6.2831854820251465f) * sigma_1);
}


#line 68
float roundedBoxShadow_0(float2 lower_0, float2 upper_0, float2 point_0, float sigma_2, float corner_1)
{

#line 68
    float2 _S10 = float2(0.5f) ;


    float2 halfSize_1 = (upper_0 - lower_0) * _S10;
    float2 p_2 = point_0 - (lower_0 + upper_0) * _S10;

    float _S11 = p_2.y;

#line 74
    float _S12 = halfSize_1.y;

#line 74
    float low_0 = _S11 - _S12;
    float high_0 = _S11 + _S12;
    float start_0 = clamp(-3.0f * sigma_2, low_0, high_0);


    float stepSize_0 = (clamp(3.0f * sigma_2, low_0, high_0) - start_0) / 4.0f;
    float _S13 = start_0 + stepSize_0 * 0.5f;

#line 80
    int i_0 = int(0);

#line 80
    float y_1 = _S13;

#line 80
    float value_0 = 0.0f;

    for(;;)
    {

#line 82
        if(i_0 < int(4))
        {
        }
        else
        {

#line 82
            break;
        }
        float value_1 = value_0 + roundedBoxShadowX_0(p_2.x, _S11 - y_1, sigma_2, corner_1, halfSize_1) * gaussian1D_0(y_1, sigma_2) * stepSize_0;
        float y_2 = y_1 + stepSize_0;

#line 82
        i_0 = i_0 + int(1);

#line 82
        y_1 = y_2;

#line 82
        value_0 = value_1;

#line 82
    }

#line 87
    return value_0;
}


#line 39 "canvas_common.slangh"
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
    float4 shadowRect_0 [[user(SHADOW_RECT)]];
    float4 borderRadius_0 [[user(BORDER_RADIUS)]];
    [[flat]] float sigma_3 [[user(SIGMA)]];
    [[flat]] uint color_0 [[user(COLOR)]];
    [[flat]] uint clipIndex_0 [[user(CLIP_INDEX)]];
};


#line 91 "canvas_shadow.slang"
[[fragment]] pixelOutput_0 fragmentMain(pixelInput_0 _S14 [[stage_in]], float4 position_0 [[position]], GlobalsData_natural_0 constant* Globals_1 [[buffer(0)]], ClipRectsData_0 constant* ClipRects_1 [[buffer(1)]])
{

#line 91
    thread KernelContext_0 kernelContext_3;

#line 91
    (&kernelContext_3)->Globals_0 = Globals_1;

#line 91
    (&kernelContext_3)->ClipRects_0 = ClipRects_1;

#line 91
    float _S15 = clipCoverage_0(_S14.pixelPos_1, _S14.clipIndex_0, &kernelContext_3);

#line 91
    bool _S16 = outsideClipRect_0(_S14.pixelPos_1, _S14.clipIndex_0, &kernelContext_3);


    if(_S16)
    {

#line 94
        discard_fragment();

#line 94
    }
    if(_S15 <= 0.0f)
    {

#line 95
        discard_fragment();

#line 95
    }

    float2 lower_1 = _S14.shadowRect_0.xy;

#line 106
    float4 rgba_0 = unpackARGB_0(_S14.color_0);

#line 106
    pixelOutput_0 _S17 = { float4(rgba_0.xyz, rgba_0.w * clamp(roundedBoxShadow_0(lower_1, lower_1 + _S14.shadowRect_0.zw, _S14.pixelPos_1, _S14.sigma_3, min(max(max(_S14.borderRadius_0.x, _S14.borderRadius_0.y), max(_S14.borderRadius_0.z, _S14.borderRadius_0.w)), min(_S14.shadowRect_0.z, _S14.shadowRect_0.w) * 0.5f)), 0.0f, 1.0f) * _S15) };
    return _S17;
}


#line 107
struct vertexMain_Result_0
{
    float4 position_1 [[position]];
    float2 pixelPos_2 [[user(PIXEL_POS)]];
    float4 shadowRect_1 [[user(SHADOW_RECT)]];
    float4 borderRadius_1 [[user(BORDER_RADIUS)]];
    float sigma_4 [[user(SIGMA)]];
    uint color_1 [[user(COLOR)]];
    uint clipIndex_1 [[user(CLIP_INDEX)]];
};


#line 107
struct vertexInput_0
{
    float2 unitPos_0 [[attribute(0)]];
    float4 outerRect_0 [[attribute(1)]];
    float4 shadowRect_2 [[attribute(2)]];
    float4 borderRadius_2 [[attribute(3)]];
    float sigma_5 [[attribute(4)]];
    uint color_2 [[attribute(5)]];
    uint clipIndex_2 [[attribute(6)]];
};


#line 18
struct Varyings_0
{
    float4 position_2;
    float2 pixelPos_3;
    float4 shadowRect_3;
    float4 borderRadius_3;
    [[flat]] float sigma_6;
    [[flat]] uint color_3;
    [[flat]] uint clipIndex_3;
};


#line 18
[[vertex]] vertexMain_Result_0 vertexMain(vertexInput_0 _S18 [[stage_in]], GlobalsData_natural_0 constant* Globals_2 [[buffer(0)]], ClipRectsData_0 constant* ClipRects_2 [[buffer(1)]])
{

#line 18
    thread KernelContext_0 kernelContext_4;

#line 18
    (&kernelContext_4)->Globals_0 = Globals_2;

#line 18
    (&kernelContext_4)->ClipRects_0 = ClipRects_2;

#line 32
    float2 pixelPos_4 = _S18.outerRect_0.xy + _S18.unitPos_0 * _S18.outerRect_0.zw;

    thread Varyings_0 o_0;

#line 34
    float4 _S19 = project_0(pixelPos_4, &kernelContext_4);
    (&o_0)->position_2 = _S19;
    (&o_0)->pixelPos_3 = pixelPos_4;
    (&o_0)->shadowRect_3 = _S18.shadowRect_2;
    (&o_0)->borderRadius_3 = _S18.borderRadius_2;
    (&o_0)->sigma_6 = _S18.sigma_5;
    (&o_0)->color_3 = _S18.color_2;
    (&o_0)->clipIndex_3 = _S18.clipIndex_2;

#line 41
    thread vertexMain_Result_0 _S20;

#line 41
    (&_S20)->position_1 = o_0.position_2;

#line 41
    (&_S20)->pixelPos_2 = o_0.pixelPos_3;

#line 41
    (&_S20)->shadowRect_1 = o_0.shadowRect_3;

#line 41
    (&_S20)->borderRadius_1 = o_0.borderRadius_3;

#line 41
    (&_S20)->sigma_4 = o_0.sigma_6;

#line 41
    (&_S20)->color_1 = o_0.color_3;

#line 41
    (&_S20)->clipIndex_1 = o_0.clipIndex_3;

#line 41
    return _S20;
}

