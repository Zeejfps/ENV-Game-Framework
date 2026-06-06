#include <metal_stdlib>
#include <metal_math>
#include <metal_texture>
using namespace metal;

#line 74 "G:/Dev/RiderProjects/ENV-Game-Framework/ZGF.Gui.Tests/Assets/Shaders/canvas_shadow.slang"
float2 erf2_0(float2 x_0)
{
    float2 _S1 = float2((vec<int,2>(sign((x_0)))));
    float2 a_0 = abs(x_0);
    float2 v_0 = float2(1.0)  + (float2(0.27839300036430359)  + (float2(0.23038899898529053)  + float2(0.07810799777507782)  * (a_0 * a_0)) * a_0) * a_0;
    float2 v_1 = v_0 * v_0;
    return _S1 - _S1 / (v_1 * v_1);
}


#line 91
float roundedBoxShadowX_0(float x_1, float y_0, float sigma_0, float corner_0, float2 halfSize_0)
{
    float _S2 = min(halfSize_0.y - corner_0 - abs(y_0), 0.0);
    float curved_0 = halfSize_0.x - corner_0 + sqrt(max(0.0, corner_0 * corner_0 - _S2 * _S2));

#line 94
    float2 _S3 = float2(0.5) ;
    float2 integral_0 = _S3 + _S3 * erf2_0(float2(x_1 - curved_0, x_1 + curved_0) * float2((sqrt(0.5) / sigma_0)) );
    return integral_0.y - integral_0.x;
}


#line 83
float gaussian1D_0(float x_2, float sigma_1)
{

    return exp(- (x_2 * x_2) / (2.0 * sigma_1 * sigma_1)) / (sqrt(6.28318548202514648) * sigma_1);
}


#line 100
float roundedBoxShadow_0(float2 lower_0, float2 upper_0, float2 point_0, float sigma_2, float corner_1)
{

#line 100
    float2 _S4 = float2(0.5) ;


    float2 halfSize_1 = (upper_0 - lower_0) * _S4;
    float2 p_0 = point_0 - (lower_0 + upper_0) * _S4;

    float _S5 = p_0.y;

#line 106
    float _S6 = halfSize_1.y;

#line 106
    float low_0 = _S5 - _S6;
    float high_0 = _S5 + _S6;
    float start_0 = clamp(-3.0 * sigma_2, low_0, high_0);


    float stepSize_0 = (clamp(3.0 * sigma_2, low_0, high_0) - start_0) / 4.0;
    float _S7 = start_0 + stepSize_0 * 0.5;

#line 112
    int i_0 = int(0);

#line 112
    float y_1 = _S7;

#line 112
    float value_0 = 0.0;

    for(;;)
    {

#line 114
        if(i_0 < int(4))
        {
        }
        else
        {

#line 114
            break;
        }
        float value_1 = value_0 + roundedBoxShadowX_0(p_0.x, _S5 - y_1, sigma_2, corner_1, halfSize_1) * gaussian1D_0(y_1, sigma_2) * stepSize_0;
        float y_2 = y_1 + stepSize_0;

#line 114
        i_0 = i_0 + int(1);

#line 114
        y_1 = y_2;

#line 114
        value_0 = value_1;

#line 114
    }

#line 119
    return value_0;
}


#line 64
float4 unpackARGB_0(uint c_0)
{

#line 70
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
    float4 shadowRect_0 [[user(TEXCOORD_1)]];
    float4 borderRadius_0 [[user(TEXCOORD_2)]];
    [[flat]] float sigma_3 [[user(TEXCOORD_3)]];
    [[flat]] uint color_0 [[user(COLOR)]];
    [[flat]] uint clipIndex_0 [[user(COLOR_1)]];
};


#line 39 "G:/Dev/RiderProjects/ENV-Game-Framework/ZGF.Gui.Tests/Assets/Shaders/canvas_shadow.slang"
struct SLANG_ParameterGroup_Globals_0
{
    matrix<float,int(4),int(4)>  u_projection_0;
};

struct SLANG_ParameterGroup_ClipRects_0
{
    array<float4, int(256)> u_clipRects_0;
};


#line 44
struct KernelContext_0
{
    SLANG_ParameterGroup_Globals_0 constant* Globals_0;
    SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_0;
};


#line 123
[[fragment]] pixelOutput_0 fragmentMain(pixelInput_0 _S8 [[stage_in]], float4 position_0 [[position]], SLANG_ParameterGroup_Globals_0 constant* Globals_1 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_1 [[buffer(1)]])
{

#line 123
    thread KernelContext_0 kernelContext_0;

#line 123
    (&kernelContext_0)->Globals_0 = Globals_1;

#line 123
    (&kernelContext_0)->ClipRects_0 = ClipRects_1;

    float4 clip_0 = ClipRects_1->u_clipRects_0[_S8.clipIndex_0];
    float _S9 = _S8.pixelPos_0.x;

#line 126
    bool _S10;

#line 126
    if(_S9 < (ClipRects_1->u_clipRects_0[_S8.clipIndex_0].x))
    {

#line 126
        _S10 = true;

#line 126
    }
    else
    {

#line 126
        _S10 = _S9 >= (clip_0.z);

#line 126
    }

#line 126
    if(_S10)
    {

#line 126
        _S10 = true;

#line 126
    }
    else
    {

#line 126
        _S10 = (_S8.pixelPos_0.y) < (clip_0.y);

#line 126
    }
    if(_S10)
    {

#line 127
        _S10 = true;

#line 127
    }
    else
    {

#line 127
        _S10 = (_S8.pixelPos_0.y) >= (clip_0.w);

#line 127
    }

#line 126
    if(_S10)
    {

        discard_fragment();

#line 126
    }

#line 132
    float2 lower_1 = _S8.shadowRect_0.xy;

#line 144
    float4 rgba_0 = unpackARGB_0(_S8.color_0);

#line 144
    pixelOutput_0 _S11 = { float4(rgba_0.xyz, rgba_0.w * clamp(roundedBoxShadow_0(lower_1, lower_1 + _S8.shadowRect_0.zw, _S8.pixelPos_0, _S8.sigma_3, min(max(max(_S8.borderRadius_0.x, _S8.borderRadius_0.y), max(_S8.borderRadius_0.z, _S8.borderRadius_0.w)), min(_S8.shadowRect_0.z, _S8.shadowRect_0.w) * 0.5)), 0.0, 1.0)) };
    return _S11;
}


#line 145
struct vertexMain_Result_0
{
    float4 position_1 [[position]];
    float2 pixelPos_1 [[user(TEXCOORD)]];
    float4 shadowRect_1 [[user(TEXCOORD_1)]];
    float4 borderRadius_1 [[user(TEXCOORD_2)]];
    float sigma_4 [[user(TEXCOORD_3)]];
    uint color_1 [[user(COLOR)]];
    uint clipIndex_1 [[user(COLOR_1)]];
};


#line 145
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


#line 28
struct Varyings_0
{
    float4 position_2;
    float2 pixelPos_2;
    float4 shadowRect_3;
    float4 borderRadius_3;
    [[flat]] float sigma_6;
    [[flat]] uint color_3;
    [[flat]] uint clipIndex_3;
};


#line 28
[[vertex]] vertexMain_Result_0 vertexMain(vertexInput_0 _S12 [[stage_in]], SLANG_ParameterGroup_Globals_0 constant* Globals_2 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_2 [[buffer(1)]])
{

#line 28
    thread KernelContext_0 kernelContext_1;

#line 28
    (&kernelContext_1)->Globals_0 = Globals_2;

#line 28
    (&kernelContext_1)->ClipRects_0 = ClipRects_2;

#line 53
    float2 pixelPos_3 = _S12.outerRect_0.xy + _S12.unitPos_0 * _S12.outerRect_0.zw;

#line 52
    thread Varyings_0 o_0;

    (&o_0)->position_2 = (((float4(pixelPos_3, 0.0, 1.0)) * (Globals_2->u_projection_0)));
    (&o_0)->pixelPos_2 = pixelPos_3;
    (&o_0)->shadowRect_3 = _S12.shadowRect_2;
    (&o_0)->borderRadius_3 = _S12.borderRadius_2;
    (&o_0)->sigma_6 = _S12.sigma_5;
    (&o_0)->color_3 = _S12.color_2;
    (&o_0)->clipIndex_3 = _S12.clipIndex_2;

#line 60
    thread vertexMain_Result_0 _S13;

#line 60
    (&_S13)->position_1 = o_0.position_2;

#line 60
    (&_S13)->pixelPos_1 = o_0.pixelPos_2;

#line 60
    (&_S13)->shadowRect_1 = o_0.shadowRect_3;

#line 60
    (&_S13)->borderRadius_1 = o_0.borderRadius_3;

#line 60
    (&_S13)->sigma_4 = o_0.sigma_6;

#line 60
    (&_S13)->color_1 = o_0.color_3;

#line 60
    (&_S13)->clipIndex_1 = o_0.clipIndex_3;

#line 60
    return _S13;
}

