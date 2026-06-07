#include <metal_stdlib>
#include <metal_math>
#include <metal_texture>
using namespace metal;

#line 89 "/tmp/shapegen4/canvas_shape.slang"
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



float bezierArcLength_0(float2 A_1, float2 B_1, float2 C_1, float t_1)
{

#line 175
    float2 _S9 = float2(2.0) ;

    float2 d0_1 = _S9 * (B_1 - A_1);
    float2 _S10 = _S9 * (C_1 - B_1);

    float _S11 = t_1 / 16.0;

#line 180
    float prev_0 = length(d0_1);

#line 180
    int i_0 = int(1);

#line 180
    float total_0 = 0.0;


    for(;;)
    {

#line 183
        if(i_0 <= int(16))
        {
        }
        else
        {

#line 183
            break;
        }
        float u_0 = t_1 * float(i_0) / 16.0;
        float cur_0 = length(d0_1 * float2((1.0 - u_0))  + _S10 * float2(u_0) );
        float total_1 = total_0 + (prev_0 + cur_0) * 0.5 * _S11;

#line 183
        int _S12 = i_0 + int(1);

#line 183
        prev_0 = cur_0;

#line 183
        i_0 = _S12;

#line 183
        total_0 = total_1;

#line 183
    }

#line 190
    return total_0;
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
    float _S13 = max(length(ba_1), 9.99999997475242708e-07);
    float2 dir_0 = ba_1 / float2(_S13) ;
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
    float _S14 = along_0 - (_S13 * 0.5 + capExt_0);

#line 111
    float _S15 = perp_0 - hw_0;
    return length(max(float2(_S14, _S15), float2(0.0) )) + min(max(_S14, _S15), 0.0);
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


#line 51 "/tmp/shapegen4/canvas_shape.slang"
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


#line 194
[[fragment]] pixelOutput_0 fragmentMain(pixelInput_0 _S16 [[stage_in]], float4 position_0 [[position]], SLANG_ParameterGroup_Globals_0 constant* Globals_1 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_1 [[buffer(1)]])
{

#line 194
    thread KernelContext_0 kernelContext_0;

#line 194
    (&kernelContext_0)->Globals_0 = Globals_1;

#line 194
    (&kernelContext_0)->ClipRects_0 = ClipRects_1;

    float4 clip_0 = ClipRects_1->u_clipRects_0[_S16.clipIndex_0];
    float _S17 = _S16.pixelPos_0.x;

#line 197
    bool _S18;

#line 197
    if(_S17 < (ClipRects_1->u_clipRects_0[_S16.clipIndex_0].x))
    {

#line 197
        _S18 = true;

#line 197
    }
    else
    {

#line 197
        _S18 = _S17 >= (clip_0.z);

#line 197
    }

#line 197
    if(_S18)
    {

#line 197
        _S18 = true;

#line 197
    }
    else
    {

#line 197
        _S18 = (_S16.pixelPos_0.y) < (clip_0.y);

#line 197
    }
    if(_S18)
    {

#line 198
        _S18 = true;

#line 198
    }
    else
    {

#line 198
        _S18 = (_S16.pixelPos_0.y) >= (clip_0.w);

#line 198
    }

#line 197
    if(_S18)
    {

        discard_fragment();

#line 197
    }

#line 205
    bool _S19 = (_S16.shapeType_0) == 3U;

#line 205
    float d_0;

#line 205
    float bezierT_0;

#line 205
    if(_S19)
    {

        float2 _S20 = _S16.shapeData_0.xy;

#line 208
        float2 _S21 = _S16.shapeData_0.zw;

#line 208
        float2 _S22 = _S16.shapeData2_0.xy;

#line 208
        float2 bz_0 = sdBezier_0(_S16.pixelPos_0, _S20, _S21, _S22);
        float _S23 = bz_0.x - _S16.halfWidth_0;
        float bezierT_1 = bz_0.y;
        if(((_S16.flags_0) & 4U) != 0U)
        {

            float _S24 = _S16.shapeData2_0.z;

#line 214
            float period_0 = _S24 + _S16.shapeData2_0.w;
            if(period_0 > 0.0)
            {
                float s_0 = bezierArcLength_0(_S20, _S21, _S22, bezierT_1);
                if((s_0 - period_0 * floor(s_0 / period_0)) > _S24)
                {

#line 219
                    discard_fragment();

#line 218
                }

#line 215
            }

#line 211
        }

#line 211
        d_0 = _S23;

#line 211
        bezierT_0 = bezierT_1;

#line 205
    }
    else
    {

#line 223
        if((_S16.shapeType_0) == 2U)
        {

            float2 _S25 = _S16.shapeData_0.xy;

#line 226
            float2 _S26 = _S16.shapeData_0.zw;

#line 226
            float _S27 = sdLineCapped_0(_S16.pixelPos_0, _S25, _S26, _S16.halfWidth_0, (_S16.flags_0) & 3U);
            if(((_S16.flags_0) & 4U) != 0U)
            {

                float2 ba_2 = _S26 - _S25;

                float s_1 = dot(_S16.pixelPos_0 - _S25, ba_2 / float2(max(length(ba_2), 9.99999997475242708e-07)) );
                float _S28 = _S16.shapeData2_0.z;

#line 233
                float period_1 = _S28 + _S16.shapeData2_0.w;
                if(period_1 > 0.0)
                {

#line 234
                    _S18 = (s_1 - period_1 * floor(s_1 / period_1)) > _S28;

#line 234
                }
                else
                {

#line 234
                    _S18 = false;

#line 234
                }

#line 234
                if(_S18)
                {

#line 235
                    discard_fragment();

#line 234
                }

#line 227
            }

#line 227
            d_0 = _S27;

#line 223
        }
        else
        {

#line 238
            if((_S16.shapeType_0) == 1U)
            {

#line 238
                d_0 = abs(length(_S16.pixelPos_0 - _S16.shapeData_0.xy) - _S16.shapeData_0.z) - _S16.halfWidth_0;

#line 238
            }
            else
            {

#line 238
                d_0 = length(_S16.pixelPos_0 - _S16.shapeData_0.xy) - _S16.shapeData_0.z;

#line 238
            }

#line 223
        }

#line 223
        bezierT_0 = 0.0;

#line 205
    }

#line 251
    float coverage_0 = clamp(0.5 - d_0 / max((fwidth((d_0))), 9.99999997475242708e-07), 0.0, 1.0);
    if(coverage_0 <= 0.0)
    {
        discard_fragment();

#line 252
    }

#line 257
    float4 rgba_0 = unpackARGB_0(_S16.color_0);

#line 257
    float4 rgba_1;
    if(((_S16.flags_0) & 8U) != 0U)
    {

#line 258
        float g_0;



        if(_S19)
        {

#line 262
            g_0 = bezierT_0;

#line 262
        }
        else
        {



            float2 _S29 = _S16.shapeData_0.xy;

#line 268
            float2 ba_3 = _S16.shapeData_0.zw - _S29;

#line 268
            g_0 = clamp(dot(_S16.pixelPos_0 - _S29, ba_3) / max(dot(ba_3, ba_3), 9.99999997475242708e-07), 0.0, 1.0);

#line 262
        }

#line 262
        rgba_1 = mix(rgba_0, unpackARGB_0(_S16.color2_0), float4(g_0) );

#line 258
    }
    else
    {

#line 258
        rgba_1 = rgba_0;

#line 258
    }

#line 258
    pixelOutput_0 _S30 = { float4(rgba_1.xyz, rgba_1.w * coverage_0) };

#line 273
    return _S30;
}


#line 273
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


#line 273
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
[[vertex]] vertexMain_Result_0 vertexMain(vertexInput_0 _S31 [[stage_in]], SLANG_ParameterGroup_Globals_0 constant* Globals_2 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_2 [[buffer(1)]])
{

#line 37
    thread KernelContext_0 kernelContext_1;

#line 37
    (&kernelContext_1)->Globals_0 = Globals_2;

#line 37
    (&kernelContext_1)->ClipRects_0 = ClipRects_2;

#line 65
    float2 pixelPos_3 = _S31.outerRect_0.xy + _S31.unitPos_0 * _S31.outerRect_0.zw;

#line 64
    thread Varyings_0 o_0;

    (&o_0)->position_2 = (((float4(pixelPos_3, 0.0, 1.0)) * (Globals_2->u_projection_0)));
    (&o_0)->pixelPos_2 = pixelPos_3;
    (&o_0)->shapeData_3 = _S31.shapeData_2;
    (&o_0)->shapeData2_3 = _S31.shapeData2_2;
    (&o_0)->halfWidth_3 = _S31.halfWidth_2;
    (&o_0)->color_3 = _S31.color_2;
    (&o_0)->shapeType_3 = _S31.shapeType_2;
    (&o_0)->clipIndex_3 = _S31.clipIndex_2;
    (&o_0)->color2_3 = _S31.color2_2;
    (&o_0)->flags_3 = _S31.flags_2;

#line 75
    thread vertexMain_Result_0 _S32;

#line 75
    (&_S32)->position_1 = o_0.position_2;

#line 75
    (&_S32)->pixelPos_1 = o_0.pixelPos_2;

#line 75
    (&_S32)->shapeData_1 = o_0.shapeData_3;

#line 75
    (&_S32)->shapeData2_1 = o_0.shapeData2_3;

#line 75
    (&_S32)->halfWidth_1 = o_0.halfWidth_3;

#line 75
    (&_S32)->color_1 = o_0.color_3;

#line 75
    (&_S32)->shapeType_1 = o_0.shapeType_3;

#line 75
    (&_S32)->clipIndex_1 = o_0.clipIndex_3;

#line 75
    (&_S32)->color2_1 = o_0.color2_3;

#line 75
    (&_S32)->flags_1 = o_0.flags_3;

#line 75
    return _S32;
}

