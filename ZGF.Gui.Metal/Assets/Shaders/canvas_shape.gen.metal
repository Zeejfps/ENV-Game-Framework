#include <metal_stdlib>
#include <metal_math>
#include <metal_texture>
using namespace metal;

#line 78 "/tmp/shapegen/canvas_shape.slang"
float sdSegment_0(float2 p_0, float2 a_0, float2 b_0)
{
    float2 pa_0 = p_0 - a_0;
    float2 ba_0 = b_0 - a_0;

    return length(pa_0 - ba_0 * float2(clamp(dot(pa_0, ba_0) / max(dot(ba_0, ba_0), 9.99999997475242708e-07), 0.0, 1.0)) );
}




float sdBezier_0(float2 p_1, float2 A_0, float2 B_0, float2 C_0)
{

#line 89
    float2 _S1 = float2(2.0) ;

    float2 b_1 = A_0 - _S1 * B_0 + C_0;


    float _S2 = dot(b_1, b_1);

#line 94
    if(_S2 < 0.00009999999747379)
    {

#line 95
        return sdSegment_0(p_1, A_0, C_0);
    }
    float2 a_1 = B_0 - A_0;
    float2 e_0 = A_0 - p_1;
    float kk_0 = 1.0 / _S2;
    float kx_0 = kk_0 * dot(a_1, b_1);
    float ky_0 = kk_0 * (2.0 * dot(a_1, a_1) + dot(e_0, b_1)) / 3.0;



    float pp_0 = ky_0 - kx_0 * kx_0;

    float q_0 = kx_0 * (2.0 * kx_0 * kx_0 - 3.0 * ky_0) + kk_0 * dot(e_0, a_1);
    float h_0 = q_0 * q_0 + 4.0 * (pp_0 * pp_0 * pp_0);

#line 108
    float res_0;

    if(h_0 >= 0.0)
    {
        float h_1 = sqrt(h_0);
        float2 x_0 = (float2(h_1, - h_1) - float2(q_0) ) / _S1;
        float2 uv_0 = float2((vec<int,2>(sign((x_0))))) * pow(abs(x_0), float2(0.3333333432674408, 0.3333333432674408));

#line 114
        float2 _S3 = float2(clamp(uv_0.x + uv_0.y - kx_0, 0.0, 1.0)) ;

        float2 dd_0 = e_0 + (_S1 * a_1 + b_1 * _S3) * _S3;

#line 116
        res_0 = dot(dd_0, dd_0);

#line 110
    }
    else
    {

#line 121
        float z_0 = sqrt(- pp_0);
        float v_0 = acos(q_0 / (pp_0 * z_0 * 2.0)) / 3.0;
        float m_0 = cos(v_0);



        float2 _S4 = _S1 * a_1;

#line 127
        float2 _S5 = float2(clamp((m_0 + m_0) * z_0 - kx_0, 0.0, 1.0)) ;

#line 127
        float2 d0_0 = e_0 + (_S4 + b_1 * _S5) * _S5;

#line 127
        float2 _S6 = float2(clamp((- (sin(v_0) * 1.73205077648162842) - m_0) * z_0 - kx_0, 0.0, 1.0)) ;
        float2 d1_0 = e_0 + (_S4 + b_1 * _S6) * _S6;

#line 128
        res_0 = min(dot(d0_0, d0_0), dot(d1_0, d1_0));

#line 110
    }

#line 131
    return sqrt(res_0);
}


#line 68
float4 unpackARGB_0(uint c_0)
{

#line 74
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
    float4 shapeData2_0 [[user(TEXCOORD_2)]];
    [[flat]] float halfWidth_0 [[user(TEXCOORD_3)]];
    [[flat]] uint color_0 [[user(COLOR)]];
    [[flat]] uint shapeType_0 [[user(COLOR_1)]];
    [[flat]] uint clipIndex_0 [[user(COLOR_2)]];
};


#line 42 "/tmp/shapegen/canvas_shape.slang"
struct SLANG_ParameterGroup_Globals_0
{
    matrix<float,int(4),int(4)>  u_projection_0;
};

struct SLANG_ParameterGroup_ClipRects_0
{
    array<float4, int(256)> u_clipRects_0;
};


#line 47
struct KernelContext_0
{
    SLANG_ParameterGroup_Globals_0 constant* Globals_0;
    SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_0;
};


#line 135
[[fragment]] pixelOutput_0 fragmentMain(pixelInput_0 _S7 [[stage_in]], float4 position_0 [[position]], SLANG_ParameterGroup_Globals_0 constant* Globals_1 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_1 [[buffer(1)]])
{

#line 135
    thread KernelContext_0 kernelContext_0;

#line 135
    (&kernelContext_0)->Globals_0 = Globals_1;

#line 135
    (&kernelContext_0)->ClipRects_0 = ClipRects_1;

    float4 clip_0 = ClipRects_1->u_clipRects_0[_S7.clipIndex_0];
    float _S8 = _S7.pixelPos_0.x;

#line 138
    bool _S9;

#line 138
    if(_S8 < (ClipRects_1->u_clipRects_0[_S7.clipIndex_0].x))
    {

#line 138
        _S9 = true;

#line 138
    }
    else
    {

#line 138
        _S9 = _S8 >= (clip_0.z);

#line 138
    }

#line 138
    if(_S9)
    {

#line 138
        _S9 = true;

#line 138
    }
    else
    {

#line 138
        _S9 = (_S7.pixelPos_0.y) < (clip_0.y);

#line 138
    }
    if(_S9)
    {

#line 139
        _S9 = true;

#line 139
    }
    else
    {

#line 139
        _S9 = (_S7.pixelPos_0.y) >= (clip_0.w);

#line 139
    }

#line 138
    if(_S9)
    {

        discard_fragment();

#line 138
    }

#line 138
    float d_0;

#line 145
    if((_S7.shapeType_0) == 3U)
    {

#line 145
        d_0 = sdBezier_0(_S7.pixelPos_0, _S7.shapeData_0.xy, _S7.shapeData_0.zw, _S7.shapeData2_0.xy) - _S7.halfWidth_0;

#line 145
    }
    else
    {


        if((_S7.shapeType_0) == 2U)
        {

#line 150
            d_0 = sdSegment_0(_S7.pixelPos_0, _S7.shapeData_0.xy, _S7.shapeData_0.zw) - _S7.halfWidth_0;

#line 150
        }
        else
        {


            if((_S7.shapeType_0) == 1U)
            {

#line 155
                d_0 = abs(length(_S7.pixelPos_0 - _S7.shapeData_0.xy) - _S7.shapeData_0.z) - _S7.halfWidth_0;

#line 155
            }
            else
            {

#line 155
                d_0 = length(_S7.pixelPos_0 - _S7.shapeData_0.xy) - _S7.shapeData_0.z;

#line 155
            }

#line 150
        }

#line 145
    }

#line 168
    float coverage_0 = clamp(0.5 - d_0 / max((fwidth((d_0))), 9.99999997475242708e-07), 0.0, 1.0);
    if(coverage_0 <= 0.0)
    {
        discard_fragment();

#line 169
    }

#line 174
    float4 rgba_0 = unpackARGB_0(_S7.color_0);

#line 174
    pixelOutput_0 _S10 = { float4(rgba_0.xyz, rgba_0.w * coverage_0) };
    return _S10;
}


#line 175
struct vertexMain_Result_0
{
    float4 position_1 [[position]];
    float2 pixelPos_1 [[user(TEXCOORD)]];
    float4 shapeData_1 [[user(TEXCOORD_1)]];
    float4 shapeData2_1 [[user(TEXCOORD_2)]];
    float halfWidth_1 [[user(TEXCOORD_3)]];
    uint color_1 [[user(COLOR)]];
    uint shapeType_1 [[user(COLOR_1)]];
    uint clipIndex_1 [[user(COLOR_2)]];
};


#line 175
struct vertexInput_0
{
    float2 unitPos_0 [[attribute(0)]];
    float4 outerRect_0 [[attribute(1)]];
    float4 shapeData_2 [[attribute(2)]];
    float4 shapeData2_2 [[attribute(3)]];
    float halfWidth_2 [[attribute(4)]];
    uint color_2 [[attribute(5)]];
    uint shapeType_2 [[attribute(6)]];
    uint clipIndex_2 [[attribute(7)]];
};


#line 30
struct Varyings_0
{
    float4 position_2;
    float2 pixelPos_2;
    float4 shapeData_3;
    float4 shapeData2_3;
    [[flat]] float halfWidth_3;
    [[flat]] uint color_3;
    [[flat]] uint shapeType_3;
    [[flat]] uint clipIndex_3;
};


#line 30
[[vertex]] vertexMain_Result_0 vertexMain(vertexInput_0 _S11 [[stage_in]], SLANG_ParameterGroup_Globals_0 constant* Globals_2 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_2 [[buffer(1)]])
{

#line 30
    thread KernelContext_0 kernelContext_1;

#line 30
    (&kernelContext_1)->Globals_0 = Globals_2;

#line 30
    (&kernelContext_1)->ClipRects_0 = ClipRects_2;

#line 56
    float2 pixelPos_3 = _S11.outerRect_0.xy + _S11.unitPos_0 * _S11.outerRect_0.zw;

#line 55
    thread Varyings_0 o_0;

    (&o_0)->position_2 = (((float4(pixelPos_3, 0.0, 1.0)) * (Globals_2->u_projection_0)));
    (&o_0)->pixelPos_2 = pixelPos_3;
    (&o_0)->shapeData_3 = _S11.shapeData_2;
    (&o_0)->shapeData2_3 = _S11.shapeData2_2;
    (&o_0)->halfWidth_3 = _S11.halfWidth_2;
    (&o_0)->color_3 = _S11.color_2;
    (&o_0)->shapeType_3 = _S11.shapeType_2;
    (&o_0)->clipIndex_3 = _S11.clipIndex_2;

#line 64
    thread vertexMain_Result_0 _S12;

#line 64
    (&_S12)->position_1 = o_0.position_2;

#line 64
    (&_S12)->pixelPos_1 = o_0.pixelPos_2;

#line 64
    (&_S12)->shapeData_1 = o_0.shapeData_3;

#line 64
    (&_S12)->shapeData2_1 = o_0.shapeData2_3;

#line 64
    (&_S12)->halfWidth_1 = o_0.halfWidth_3;

#line 64
    (&_S12)->color_1 = o_0.color_3;

#line 64
    (&_S12)->shapeType_1 = o_0.shapeType_3;

#line 64
    (&_S12)->clipIndex_1 = o_0.clipIndex_3;

#line 64
    return _S12;
}

