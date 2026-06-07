#include <metal_stdlib>
#include <metal_math>
#include <metal_texture>
using namespace metal;

#line 73 "/tmp/shapegen/canvas_shape.slang"
float sdSegment_0(float2 p_0, float2 a_0, float2 b_0)
{
    float2 pa_0 = p_0 - a_0;
    float2 ba_0 = b_0 - a_0;

    return length(pa_0 - ba_0 * float2(clamp(dot(pa_0, ba_0) / max(dot(ba_0, ba_0), 9.99999997475242708e-07), 0.0, 1.0)) );
}


#line 63
float4 unpackARGB_0(uint c_0)
{

#line 69
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
    float4 shapeData_0 [[user(TEXCOORD_1)]];
    [[flat]] float halfWidth_0 [[user(TEXCOORD_2)]];
    [[flat]] uint color_0 [[user(COLOR)]];
    [[flat]] uint shapeType_0 [[user(COLOR_1)]];
    [[flat]] uint clipIndex_0 [[user(COLOR_2)]];
};


#line 38 "/tmp/shapegen/canvas_shape.slang"
struct SLANG_ParameterGroup_Globals_0
{
    matrix<float,int(4),int(4)>  u_projection_0;
};

struct SLANG_ParameterGroup_ClipRects_0
{
    array<float4, int(256)> u_clipRects_0;
};


#line 43
struct KernelContext_0
{
    SLANG_ParameterGroup_Globals_0 constant* Globals_0;
    SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_0;
};


#line 82
[[fragment]] pixelOutput_0 fragmentMain(pixelInput_0 _S1 [[stage_in]], float4 position_0 [[position]], SLANG_ParameterGroup_Globals_0 constant* Globals_1 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_1 [[buffer(1)]])
{

#line 82
    thread KernelContext_0 kernelContext_0;

#line 82
    (&kernelContext_0)->Globals_0 = Globals_1;

#line 82
    (&kernelContext_0)->ClipRects_0 = ClipRects_1;

    float4 clip_0 = ClipRects_1->u_clipRects_0[_S1.clipIndex_0];
    float _S2 = _S1.pixelPos_0.x;

#line 85
    bool _S3;

#line 85
    if(_S2 < (ClipRects_1->u_clipRects_0[_S1.clipIndex_0].x))
    {

#line 85
        _S3 = true;

#line 85
    }
    else
    {

#line 85
        _S3 = _S2 >= (clip_0.z);

#line 85
    }

#line 85
    if(_S3)
    {

#line 85
        _S3 = true;

#line 85
    }
    else
    {

#line 85
        _S3 = (_S1.pixelPos_0.y) < (clip_0.y);

#line 85
    }
    if(_S3)
    {

#line 86
        _S3 = true;

#line 86
    }
    else
    {

#line 86
        _S3 = (_S1.pixelPos_0.y) >= (clip_0.w);

#line 86
    }

#line 85
    if(_S3)
    {

        discard_fragment();

#line 85
    }

#line 85
    float d_0;

#line 92
    if((_S1.shapeType_0) == 2U)
    {

#line 92
        d_0 = sdSegment_0(_S1.pixelPos_0, _S1.shapeData_0.xy, _S1.shapeData_0.zw) - _S1.halfWidth_0;

#line 92
    }
    else
    {


        if((_S1.shapeType_0) == 1U)
        {

#line 97
            d_0 = abs(length(_S1.pixelPos_0 - _S1.shapeData_0.xy) - _S1.shapeData_0.z) - _S1.halfWidth_0;

#line 97
        }
        else
        {

#line 97
            d_0 = length(_S1.pixelPos_0 - _S1.shapeData_0.xy) - _S1.shapeData_0.z;

#line 97
        }

#line 92
    }

#line 110
    float coverage_0 = clamp(0.5 - d_0 / max((fwidth((d_0))), 9.99999997475242708e-07), 0.0, 1.0);
    if(coverage_0 <= 0.0)
    {
        discard_fragment();

#line 111
    }

#line 116
    float4 rgba_0 = unpackARGB_0(_S1.color_0);

#line 116
    pixelOutput_0 _S4 = { float4(rgba_0.xyz, rgba_0.w * coverage_0) };
    return _S4;
}


#line 117
struct vertexMain_Result_0
{
    float4 position_1 [[position]];
    float2 pixelPos_1 [[user(TEXCOORD)]];
    float4 shapeData_1 [[user(TEXCOORD_1)]];
    float halfWidth_1 [[user(TEXCOORD_2)]];
    uint color_1 [[user(COLOR)]];
    uint shapeType_1 [[user(COLOR_1)]];
    uint clipIndex_1 [[user(COLOR_2)]];
};


#line 117
struct vertexInput_0
{
    float2 unitPos_0 [[attribute(0)]];
    float4 outerRect_0 [[attribute(1)]];
    float4 shapeData_2 [[attribute(2)]];
    float halfWidth_2 [[attribute(3)]];
    uint color_2 [[attribute(4)]];
    uint shapeType_2 [[attribute(5)]];
    uint clipIndex_2 [[attribute(6)]];
};


#line 27
struct Varyings_0
{
    float4 position_2;
    float2 pixelPos_2;
    float4 shapeData_3;
    [[flat]] float halfWidth_3;
    [[flat]] uint color_3;
    [[flat]] uint shapeType_3;
    [[flat]] uint clipIndex_3;
};


#line 27
[[vertex]] vertexMain_Result_0 vertexMain(vertexInput_0 _S5 [[stage_in]], SLANG_ParameterGroup_Globals_0 constant* Globals_2 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_2 [[buffer(1)]])
{

#line 27
    thread KernelContext_0 kernelContext_1;

#line 27
    (&kernelContext_1)->Globals_0 = Globals_2;

#line 27
    (&kernelContext_1)->ClipRects_0 = ClipRects_2;

#line 52
    float2 pixelPos_3 = _S5.outerRect_0.xy + _S5.unitPos_0 * _S5.outerRect_0.zw;

#line 51
    thread Varyings_0 o_0;

    (&o_0)->position_2 = (((float4(pixelPos_3, 0.0, 1.0)) * (Globals_2->u_projection_0)));
    (&o_0)->pixelPos_2 = pixelPos_3;
    (&o_0)->shapeData_3 = _S5.shapeData_2;
    (&o_0)->halfWidth_3 = _S5.halfWidth_2;
    (&o_0)->color_3 = _S5.color_2;
    (&o_0)->shapeType_3 = _S5.shapeType_2;
    (&o_0)->clipIndex_3 = _S5.clipIndex_2;

#line 59
    thread vertexMain_Result_0 _S6;

#line 59
    (&_S6)->position_1 = o_0.position_2;

#line 59
    (&_S6)->pixelPos_1 = o_0.pixelPos_2;

#line 59
    (&_S6)->shapeData_1 = o_0.shapeData_3;

#line 59
    (&_S6)->halfWidth_1 = o_0.halfWidth_3;

#line 59
    (&_S6)->color_1 = o_0.color_3;

#line 59
    (&_S6)->shapeType_1 = o_0.shapeType_3;

#line 59
    (&_S6)->clipIndex_1 = o_0.clipIndex_3;

#line 59
    return _S6;
}

