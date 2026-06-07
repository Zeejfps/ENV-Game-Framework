#include <metal_stdlib>
#include <metal_math>
#include <metal_texture>
using namespace metal;

#line 89 "/tmp/shapegen3/canvas_shape.slang"
float sdSegment_0(float2 p_0, float2 a_0, float2 b_0)
{
    float2 pa_0 = p_0 - a_0;
    float2 ba_0 = b_0 - a_0;

    return length(pa_0 - ba_0 * float2(clamp(dot(pa_0, ba_0) / max(dot(ba_0, ba_0), 9.99999997475242708e-07), 0.0, 1.0)) );
}


#line 119
float2 sdBezier_0(float2 p_1, float2 A_0, float2 B_0, float2 C_0)
{

#line 119
    float2 _S1 = float2(2.0) ;

    float2 b_1 = A_0 - _S1 * B_0 + C_0;


    float _S2 = dot(b_1, b_1);

#line 124
    if(_S2 < 0.00009999999747379)
    {
        float2 ac_0 = C_0 - A_0;

        return float2(sdSegment_0(p_1, A_0, C_0), clamp(dot(p_1 - A_0, ac_0) / max(dot(ac_0, ac_0), 9.99999997475242708e-07), 0.0, 1.0));
    }

    float2 a_1 = B_0 - A_0;
    float2 e_0 = A_0 - p_1;
    float kk_0 = 1.0 / _S2;
    float kx_0 = kk_0 * dot(a_1, b_1);
    float ky_0 = kk_0 * (2.0 * dot(a_1, a_1) + dot(e_0, b_1)) / 3.0;

#line 140
    float pp_0 = ky_0 - kx_0 * kx_0;

    float q_0 = kx_0 * (2.0 * kx_0 * kx_0 - 3.0 * ky_0) + kk_0 * dot(e_0, a_1);
    float h_0 = q_0 * q_0 + 4.0 * (pp_0 * pp_0 * pp_0);

#line 143
    float res_0;

#line 143
    float bestT_0;

    if(h_0 >= 0.0)
    {
        float h_1 = sqrt(h_0);
        float2 x_0 = (float2(h_1, - h_1) - float2(q_0) ) / _S1;
        float2 uv_0 = float2((vec<int,2>(sign((x_0))))) * pow(abs(x_0), float2(0.3333333432674408, 0.3333333432674408));
        float t_0 = clamp(uv_0.x + uv_0.y - kx_0, 0.0, 1.0);

#line 150
        float2 _S3 = float2(t_0) ;
        float2 dd_0 = e_0 + (_S1 * a_1 + b_1 * _S3) * _S3;

#line 151
        res_0 = dot(dd_0, dd_0);

#line 151
        bestT_0 = t_0;

#line 145
    }
    else
    {

#line 157
        float z_0 = sqrt(- pp_0);
        float v_0 = acos(q_0 / (pp_0 * z_0 * 2.0)) / 3.0;
        float m_0 = cos(v_0);

        float t0_0 = clamp((m_0 + m_0) * z_0 - kx_0, 0.0, 1.0);
        float t1_0 = clamp((- (sin(v_0) * 1.73205077648162842) - m_0) * z_0 - kx_0, 0.0, 1.0);
        float2 _S4 = _S1 * a_1;

#line 163
        float2 _S5 = float2(t0_0) ;

#line 163
        float2 d0_0 = e_0 + (_S4 + b_1 * _S5) * _S5;

#line 163
        float2 _S6 = float2(t1_0) ;
        float2 d1_0 = e_0 + (_S4 + b_1 * _S6) * _S6;
        float dd0_0 = dot(d0_0, d0_0);
        float dd1_0 = dot(d1_0, d1_0);
        float _S7 = min(dd0_0, dd1_0);
        if(dd0_0 < dd1_0)
        {

#line 168
            res_0 = t0_0;

#line 168
        }
        else
        {

#line 168
            res_0 = t1_0;

#line 168
        }

#line 168
        float _S8 = res_0;

#line 168
        res_0 = _S7;

#line 168
        bestT_0 = _S8;

#line 145
    }

#line 170
    return float2(sqrt(res_0), bestT_0);
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
    float _S9 = max(length(ba_1), 9.99999997475242708e-07);
    float2 dir_0 = ba_1 / float2(_S9) ;
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
    float _S10 = along_0 - (_S9 * 0.5 + capExt_0);

#line 111
    float _S11 = perp_0 - hw_0;
    return length(max(float2(_S10, _S11), float2(0.0) )) + min(max(_S10, _S11), 0.0);
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


#line 51 "/tmp/shapegen3/canvas_shape.slang"
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


#line 174
[[fragment]] pixelOutput_0 fragmentMain(pixelInput_0 _S12 [[stage_in]], float4 position_0 [[position]], SLANG_ParameterGroup_Globals_0 constant* Globals_1 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_1 [[buffer(1)]])
{

#line 174
    thread KernelContext_0 kernelContext_0;

#line 174
    (&kernelContext_0)->Globals_0 = Globals_1;

#line 174
    (&kernelContext_0)->ClipRects_0 = ClipRects_1;

    float4 clip_0 = ClipRects_1->u_clipRects_0[_S12.clipIndex_0];
    float _S13 = _S12.pixelPos_0.x;

#line 177
    bool _S14;

#line 177
    if(_S13 < (ClipRects_1->u_clipRects_0[_S12.clipIndex_0].x))
    {

#line 177
        _S14 = true;

#line 177
    }
    else
    {

#line 177
        _S14 = _S13 >= (clip_0.z);

#line 177
    }

#line 177
    if(_S14)
    {

#line 177
        _S14 = true;

#line 177
    }
    else
    {

#line 177
        _S14 = (_S12.pixelPos_0.y) < (clip_0.y);

#line 177
    }
    if(_S14)
    {

#line 178
        _S14 = true;

#line 178
    }
    else
    {

#line 178
        _S14 = (_S12.pixelPos_0.y) >= (clip_0.w);

#line 178
    }

#line 177
    if(_S14)
    {

        discard_fragment();

#line 177
    }

#line 185
    bool _S15 = (_S12.shapeType_0) == 3U;

#line 185
    float d_0;

#line 185
    float bezierT_0;

#line 185
    if(_S15)
    {

        float2 bz_0 = sdBezier_0(_S12.pixelPos_0, _S12.shapeData_0.xy, _S12.shapeData_0.zw, _S12.shapeData2_0.xy);

        float _S16 = bz_0.y;

#line 190
        d_0 = bz_0.x - _S12.halfWidth_0;

#line 190
        bezierT_0 = _S16;

#line 185
    }
    else
    {

#line 192
        if((_S12.shapeType_0) == 2U)
        {

            float2 _S17 = _S12.shapeData_0.xy;

#line 195
            float2 _S18 = _S12.shapeData_0.zw;

#line 195
            float _S19 = sdLineCapped_0(_S12.pixelPos_0, _S17, _S18, _S12.halfWidth_0, (_S12.flags_0) & 3U);
            if(((_S12.flags_0) & 4U) != 0U)
            {

                float2 ba_2 = _S18 - _S17;

                float s_0 = dot(_S12.pixelPos_0 - _S17, ba_2 / float2(max(length(ba_2), 9.99999997475242708e-07)) );
                float _S20 = _S12.shapeData2_0.z;

#line 202
                float period_0 = _S20 + _S12.shapeData2_0.w;
                if(period_0 > 0.0)
                {

#line 203
                    _S14 = (s_0 - period_0 * floor(s_0 / period_0)) > _S20;

#line 203
                }
                else
                {

#line 203
                    _S14 = false;

#line 203
                }

#line 203
                if(_S14)
                {

#line 204
                    discard_fragment();

#line 203
                }

#line 196
            }

#line 196
            d_0 = _S19;

#line 192
        }
        else
        {

#line 207
            if((_S12.shapeType_0) == 1U)
            {

#line 207
                d_0 = abs(length(_S12.pixelPos_0 - _S12.shapeData_0.xy) - _S12.shapeData_0.z) - _S12.halfWidth_0;

#line 207
            }
            else
            {

#line 207
                d_0 = length(_S12.pixelPos_0 - _S12.shapeData_0.xy) - _S12.shapeData_0.z;

#line 207
            }

#line 192
        }

#line 192
        bezierT_0 = 0.0;

#line 185
    }

#line 220
    float coverage_0 = clamp(0.5 - d_0 / max((fwidth((d_0))), 9.99999997475242708e-07), 0.0, 1.0);
    if(coverage_0 <= 0.0)
    {
        discard_fragment();

#line 221
    }

#line 226
    float4 rgba_0 = unpackARGB_0(_S12.color_0);

#line 226
    float4 rgba_1;
    if(((_S12.flags_0) & 8U) != 0U)
    {

#line 227
        float g_0;



        if(_S15)
        {

#line 231
            g_0 = bezierT_0;

#line 231
        }
        else
        {



            float2 _S21 = _S12.shapeData_0.xy;

#line 237
            float2 ba_3 = _S12.shapeData_0.zw - _S21;

#line 237
            g_0 = clamp(dot(_S12.pixelPos_0 - _S21, ba_3) / max(dot(ba_3, ba_3), 9.99999997475242708e-07), 0.0, 1.0);

#line 231
        }

#line 231
        rgba_1 = mix(rgba_0, unpackARGB_0(_S12.color2_0), float4(g_0) );

#line 227
    }
    else
    {

#line 227
        rgba_1 = rgba_0;

#line 227
    }

#line 227
    pixelOutput_0 _S22 = { float4(rgba_1.xyz, rgba_1.w * coverage_0) };

#line 242
    return _S22;
}


#line 242
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


#line 242
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
[[vertex]] vertexMain_Result_0 vertexMain(vertexInput_0 _S23 [[stage_in]], SLANG_ParameterGroup_Globals_0 constant* Globals_2 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_2 [[buffer(1)]])
{

#line 37
    thread KernelContext_0 kernelContext_1;

#line 37
    (&kernelContext_1)->Globals_0 = Globals_2;

#line 37
    (&kernelContext_1)->ClipRects_0 = ClipRects_2;

#line 65
    float2 pixelPos_3 = _S23.outerRect_0.xy + _S23.unitPos_0 * _S23.outerRect_0.zw;

#line 64
    thread Varyings_0 o_0;

    (&o_0)->position_2 = (((float4(pixelPos_3, 0.0, 1.0)) * (Globals_2->u_projection_0)));
    (&o_0)->pixelPos_2 = pixelPos_3;
    (&o_0)->shapeData_3 = _S23.shapeData_2;
    (&o_0)->shapeData2_3 = _S23.shapeData2_2;
    (&o_0)->halfWidth_3 = _S23.halfWidth_2;
    (&o_0)->color_3 = _S23.color_2;
    (&o_0)->shapeType_3 = _S23.shapeType_2;
    (&o_0)->clipIndex_3 = _S23.clipIndex_2;
    (&o_0)->color2_3 = _S23.color2_2;
    (&o_0)->flags_3 = _S23.flags_2;

#line 75
    thread vertexMain_Result_0 _S24;

#line 75
    (&_S24)->position_1 = o_0.position_2;

#line 75
    (&_S24)->pixelPos_1 = o_0.pixelPos_2;

#line 75
    (&_S24)->shapeData_1 = o_0.shapeData_3;

#line 75
    (&_S24)->shapeData2_1 = o_0.shapeData2_3;

#line 75
    (&_S24)->halfWidth_1 = o_0.halfWidth_3;

#line 75
    (&_S24)->color_1 = o_0.color_3;

#line 75
    (&_S24)->shapeType_1 = o_0.shapeType_3;

#line 75
    (&_S24)->clipIndex_1 = o_0.clipIndex_3;

#line 75
    (&_S24)->color2_1 = o_0.color2_3;

#line 75
    (&_S24)->flags_1 = o_0.flags_3;

#line 75
    return _S24;
}

