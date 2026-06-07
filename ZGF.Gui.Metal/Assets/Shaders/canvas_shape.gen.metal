#include <metal_stdlib>
#include <metal_math>
#include <metal_texture>
using namespace metal;

#line 89 "/tmp/shapegen2/canvas_shape.slang"
float sdSegment_0(float2 p_0, float2 a_0, float2 b_0)
{
    float2 pa_0 = p_0 - a_0;
    float2 ba_0 = b_0 - a_0;

    return length(pa_0 - ba_0 * float2(clamp(dot(pa_0, ba_0) / max(dot(ba_0, ba_0), 9.99999997475242708e-07), 0.0, 1.0)) );
}


#line 118
float sdBezier_0(float2 p_1, float2 A_0, float2 B_0, float2 C_0)
{

#line 118
    float2 _S1 = float2(2.0) ;

    float2 b_1 = A_0 - _S1 * B_0 + C_0;


    float _S2 = dot(b_1, b_1);

#line 123
    if(_S2 < 0.00009999999747379)
    {

#line 124
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

#line 137
    float res_0;

    if(h_0 >= 0.0)
    {
        float h_1 = sqrt(h_0);
        float2 x_0 = (float2(h_1, - h_1) - float2(q_0) ) / _S1;
        float2 uv_0 = float2((vec<int,2>(sign((x_0))))) * pow(abs(x_0), float2(0.3333333432674408, 0.3333333432674408));

#line 143
        float2 _S3 = float2(clamp(uv_0.x + uv_0.y - kx_0, 0.0, 1.0)) ;

        float2 dd_0 = e_0 + (_S1 * a_1 + b_1 * _S3) * _S3;

#line 145
        res_0 = dot(dd_0, dd_0);

#line 139
    }
    else
    {

#line 150
        float z_0 = sqrt(- pp_0);
        float v_0 = acos(q_0 / (pp_0 * z_0 * 2.0)) / 3.0;
        float m_0 = cos(v_0);



        float2 _S4 = _S1 * a_1;

#line 156
        float2 _S5 = float2(clamp((m_0 + m_0) * z_0 - kx_0, 0.0, 1.0)) ;

#line 156
        float2 d0_0 = e_0 + (_S4 + b_1 * _S5) * _S5;

#line 156
        float2 _S6 = float2(clamp((- (sin(v_0) * 1.73205077648162842) - m_0) * z_0 - kx_0, 0.0, 1.0)) ;
        float2 d1_0 = e_0 + (_S4 + b_1 * _S6) * _S6;

#line 157
        res_0 = min(dot(d0_0, d0_0), dot(d1_0, d1_0));

#line 139
    }

#line 160
    return sqrt(res_0);
}


#line 99
float sdLineCapped_0(float2 p_2, float2 a_2, float2 b_2, float hw_0, uint cap_0)
{
    if(cap_0 == 0U)
    {

#line 102
        return sdSegment_0(p_2, a_2, b_2) - hw_0;
    }
    float2 ba_1 = b_2 - a_2;
    float _S7 = max(length(ba_1), 9.99999997475242708e-07);
    float2 dir_0 = ba_1 / float2(_S7) ;
    float2 rel_0 = p_2 - (a_2 + b_2) * float2(0.5) ;
    float along_0 = abs(dot(rel_0, dir_0));
    float perp_0 = abs(dot(rel_0, float2(- dir_0.y, dir_0.x)));

#line 109
    float capExt_0;
    if(cap_0 == 2U)
    {

#line 110
        capExt_0 = hw_0;

#line 110
    }
    else
    {

#line 110
        capExt_0 = 0.0;

#line 110
    }
    float _S8 = along_0 - (_S7 * 0.5 + capExt_0);

#line 111
    float _S9 = perp_0 - hw_0;
    return length(max(float2(_S8, _S9), float2(0.0) )) + min(max(_S8, _S9), 0.0);
}


#line 79
float4 unpackARGB_0(uint c_0)
{

#line 85
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
    [[flat]] uint color2_0 [[user(COLOR_3)]];
    [[flat]] uint flags_0 [[user(COLOR_4)]];
};


#line 51 "/tmp/shapegen2/canvas_shape.slang"
struct SLANG_ParameterGroup_Globals_0
{
    matrix<float,int(4),int(4)>  u_projection_0;
};

struct SLANG_ParameterGroup_ClipRects_0
{
    array<float4, int(256)> u_clipRects_0;
};


#line 56
struct KernelContext_0
{
    SLANG_ParameterGroup_Globals_0 constant* Globals_0;
    SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_0;
};


#line 164
[[fragment]] pixelOutput_0 fragmentMain(pixelInput_0 _S10 [[stage_in]], float4 position_0 [[position]], SLANG_ParameterGroup_Globals_0 constant* Globals_1 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_1 [[buffer(1)]])
{

#line 164
    thread KernelContext_0 kernelContext_0;

#line 164
    (&kernelContext_0)->Globals_0 = Globals_1;

#line 164
    (&kernelContext_0)->ClipRects_0 = ClipRects_1;

    float4 clip_0 = ClipRects_1->u_clipRects_0[_S10.clipIndex_0];
    float _S11 = _S10.pixelPos_0.x;

#line 167
    bool _S12;

#line 167
    if(_S11 < (ClipRects_1->u_clipRects_0[_S10.clipIndex_0].x))
    {

#line 167
        _S12 = true;

#line 167
    }
    else
    {

#line 167
        _S12 = _S11 >= (clip_0.z);

#line 167
    }

#line 167
    if(_S12)
    {

#line 167
        _S12 = true;

#line 167
    }
    else
    {

#line 167
        _S12 = (_S10.pixelPos_0.y) < (clip_0.y);

#line 167
    }
    if(_S12)
    {

#line 168
        _S12 = true;

#line 168
    }
    else
    {

#line 168
        _S12 = (_S10.pixelPos_0.y) >= (clip_0.w);

#line 168
    }

#line 167
    if(_S12)
    {

        discard_fragment();

#line 167
    }

#line 167
    float d_0;

#line 174
    if((_S10.shapeType_0) == 3U)
    {

#line 174
        d_0 = sdBezier_0(_S10.pixelPos_0, _S10.shapeData_0.xy, _S10.shapeData_0.zw, _S10.shapeData2_0.xy) - _S10.halfWidth_0;

#line 174
    }
    else
    {


        if((_S10.shapeType_0) == 2U)
        {

            float2 _S13 = _S10.shapeData_0.xy;

#line 182
            float2 _S14 = _S10.shapeData_0.zw;

#line 182
            float _S15 = sdLineCapped_0(_S10.pixelPos_0, _S13, _S14, _S10.halfWidth_0, (_S10.flags_0) & 3U);
            if(((_S10.flags_0) & 4U) != 0U)
            {

                float2 ba_2 = _S14 - _S13;

                float s_0 = dot(_S10.pixelPos_0 - _S13, ba_2 / float2(max(length(ba_2), 9.99999997475242708e-07)) );
                float _S16 = _S10.shapeData2_0.z;

#line 189
                float period_0 = _S16 + _S10.shapeData2_0.w;
                if(period_0 > 0.0)
                {

#line 190
                    _S12 = (s_0 - period_0 * floor(s_0 / period_0)) > _S16;

#line 190
                }
                else
                {

#line 190
                    _S12 = false;

#line 190
                }

#line 190
                if(_S12)
                {

#line 191
                    discard_fragment();

#line 190
                }

#line 183
            }

#line 183
            d_0 = _S15;

#line 179
        }
        else
        {

#line 194
            if((_S10.shapeType_0) == 1U)
            {

#line 194
                d_0 = abs(length(_S10.pixelPos_0 - _S10.shapeData_0.xy) - _S10.shapeData_0.z) - _S10.halfWidth_0;

#line 194
            }
            else
            {

#line 194
                d_0 = length(_S10.pixelPos_0 - _S10.shapeData_0.xy) - _S10.shapeData_0.z;

#line 194
            }

#line 179
        }

#line 174
    }

#line 207
    float coverage_0 = clamp(0.5 - d_0 / max((fwidth((d_0))), 9.99999997475242708e-07), 0.0, 1.0);
    if(coverage_0 <= 0.0)
    {
        discard_fragment();

#line 208
    }

#line 213
    float4 rgba_0 = unpackARGB_0(_S10.color_0);
    if((_S10.shapeType_0) == 2U)
    {

#line 214
        _S12 = ((_S10.flags_0) & 8U) != 0U;

#line 214
    }
    else
    {

#line 214
        _S12 = false;

#line 214
    }

#line 214
    float4 rgba_1;

#line 214
    if(_S12)
    {

        float2 _S17 = _S10.shapeData_0.xy;

#line 217
        float2 ba_3 = _S10.shapeData_0.zw - _S17;

#line 217
        rgba_1 = mix(rgba_0, unpackARGB_0(_S10.color2_0), float4(clamp(dot(_S10.pixelPos_0 - _S17, ba_3) / max(dot(ba_3, ba_3), 9.99999997475242708e-07), 0.0, 1.0)) );

#line 214
    }
    else
    {

#line 214
        rgba_1 = rgba_0;

#line 214
    }

#line 214
    pixelOutput_0 _S18 = { float4(rgba_1.xyz, rgba_1.w * coverage_0) };

#line 221
    return _S18;
}


#line 221
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
    uint color2_1 [[user(COLOR_3)]];
    uint flags_1 [[user(COLOR_4)]];
};


#line 221
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
    uint color2_2 [[attribute(8)]];
    uint flags_2 [[attribute(9)]];
};


#line 37
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
    [[flat]] uint color2_3;
    [[flat]] uint flags_3;
};


#line 37
[[vertex]] vertexMain_Result_0 vertexMain(vertexInput_0 _S19 [[stage_in]], SLANG_ParameterGroup_Globals_0 constant* Globals_2 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_2 [[buffer(1)]])
{

#line 37
    thread KernelContext_0 kernelContext_1;

#line 37
    (&kernelContext_1)->Globals_0 = Globals_2;

#line 37
    (&kernelContext_1)->ClipRects_0 = ClipRects_2;

#line 65
    float2 pixelPos_3 = _S19.outerRect_0.xy + _S19.unitPos_0 * _S19.outerRect_0.zw;

#line 64
    thread Varyings_0 o_0;

    (&o_0)->position_2 = (((float4(pixelPos_3, 0.0, 1.0)) * (Globals_2->u_projection_0)));
    (&o_0)->pixelPos_2 = pixelPos_3;
    (&o_0)->shapeData_3 = _S19.shapeData_2;
    (&o_0)->shapeData2_3 = _S19.shapeData2_2;
    (&o_0)->halfWidth_3 = _S19.halfWidth_2;
    (&o_0)->color_3 = _S19.color_2;
    (&o_0)->shapeType_3 = _S19.shapeType_2;
    (&o_0)->clipIndex_3 = _S19.clipIndex_2;
    (&o_0)->color2_3 = _S19.color2_2;
    (&o_0)->flags_3 = _S19.flags_2;

#line 75
    thread vertexMain_Result_0 _S20;

#line 75
    (&_S20)->position_1 = o_0.position_2;

#line 75
    (&_S20)->pixelPos_1 = o_0.pixelPos_2;

#line 75
    (&_S20)->shapeData_1 = o_0.shapeData_3;

#line 75
    (&_S20)->shapeData2_1 = o_0.shapeData2_3;

#line 75
    (&_S20)->halfWidth_1 = o_0.halfWidth_3;

#line 75
    (&_S20)->color_1 = o_0.color_3;

#line 75
    (&_S20)->shapeType_1 = o_0.shapeType_3;

#line 75
    (&_S20)->clipIndex_1 = o_0.clipIndex_3;

#line 75
    (&_S20)->color2_1 = o_0.color2_3;

#line 75
    (&_S20)->flags_1 = o_0.flags_3;

#line 75
    return _S20;
}

