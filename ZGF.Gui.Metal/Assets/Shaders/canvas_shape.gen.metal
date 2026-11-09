// Generated from ZGF.Gui.Shaders/canvas_shape.slang by ZGF.Gui.Shaders/regenerate.cs. Edit the .slang and regenerate.
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


#line 52 "canvas_shape.slang"
float sdSegment_0(float2 p_2, float2 a_0, float2 b_0)
{
    float2 pa_0 = p_2 - a_0;
    float2 ba_0 = b_0 - a_0;

    return length(pa_0 - ba_0 * float2(clamp(dot(pa_0, ba_0) / max(dot(ba_0, ba_0), 9.99999997475242708e-07f), 0.0f, 1.0f)) );
}


#line 77
float2 sdBezier_0(float2 p_3, float2 A_0, float2 B_0, float2 C_0)
{

#line 77
    float2 _S7 = float2(2.0f) ;

    float2 b_1 = A_0 - _S7 * B_0 + C_0;
    float _S8 = dot(b_1, b_1);

#line 80
    if(_S8 < 9.9999997473787516e-05f)
    {

        float2 ac_0 = C_0 - A_0;

        return float2(sdSegment_0(p_3, A_0, C_0), clamp(dot(p_3 - A_0, ac_0) / max(dot(ac_0, ac_0), 9.99999997475242708e-07f), 0.0f, 1.0f));
    }
    float2 a_1 = B_0 - A_0;
    float2 e_0 = A_0 - p_3;
    float kk_0 = 1.0f / _S8;
    float kx_0 = kk_0 * dot(a_1, b_1);
    float ky_0 = kk_0 * (2.0f * dot(a_1, a_1) + dot(e_0, b_1)) / 3.0f;



    float pp_0 = ky_0 - kx_0 * kx_0;

    float q_1 = kx_0 * (2.0f * kx_0 * kx_0 - 3.0f * ky_0) + kk_0 * dot(e_0, a_1);
    float h_0 = q_1 * q_1 + 4.0f * (pp_0 * pp_0 * pp_0);

#line 98
    float res_0;

#line 98
    float bestT_0;
    if(h_0 >= 0.0f)
    {
        float h_1 = sqrt(h_0);
        float2 x_0 = (float2(h_1, - h_1) - float2(q_1) ) / _S7;
        float2 uv_0 = float2((vec<int,2>(sign((x_0))))) * pow(abs(x_0), float2(0.3333333432674408f) );
        float t_0 = clamp(uv_0.x + uv_0.y - kx_0, 0.0f, 1.0f);

#line 104
        float2 _S9 = float2(t_0) ;
        float2 dd_0 = e_0 + (_S7 * a_1 + b_1 * _S9) * _S9;

#line 105
        res_0 = dot(dd_0, dd_0);

#line 105
        bestT_0 = t_0;

#line 99
    }
    else
    {

#line 111
        float z_0 = sqrt(- pp_0);
        float v_0 = acos(q_1 / (pp_0 * z_0 * 2.0f)) / 3.0f;
        float m_0 = cos(v_0);

        float t0_0 = clamp((m_0 + m_0) * z_0 - kx_0, 0.0f, 1.0f);
        float t1_0 = clamp((- (sin(v_0) * 1.7320507764816284f) - m_0) * z_0 - kx_0, 0.0f, 1.0f);
        float2 _S10 = _S7 * a_1;

#line 117
        float2 _S11 = float2(t0_0) ;

#line 117
        float2 d0_0 = e_0 + (_S10 + b_1 * _S11) * _S11;

#line 117
        float2 _S12 = float2(t1_0) ;
        float2 d1_0 = e_0 + (_S10 + b_1 * _S12) * _S12;
        float dd0_0 = dot(d0_0, d0_0);
        float dd1_0 = dot(d1_0, d1_0);
        float _S13 = min(dd0_0, dd1_0);
        if(dd0_0 < dd1_0)
        {

#line 122
            res_0 = t0_0;

#line 122
        }
        else
        {

#line 122
            res_0 = t1_0;

#line 122
        }

#line 122
        float _S14 = res_0;

#line 122
        res_0 = _S13;

#line 122
        bestT_0 = _S14;

#line 99
    }

#line 124
    return float2(sqrt(res_0), bestT_0);
}


float bezierArcLength_0(float2 A_1, float2 B_1, float2 C_1, float t_1)
{

#line 128
    float2 _S15 = float2(2.0f) ;

    float2 d0_1 = _S15 * (B_1 - A_1);
    float2 _S16 = _S15 * (C_1 - B_1);

    float _S17 = t_1 / 16.0f;

#line 133
    float prev_0 = length(d0_1);

#line 133
    int i_0 = int(1);

#line 133
    float total_0 = 0.0f;


    for(;;)
    {

#line 136
        if(i_0 <= int(16))
        {
        }
        else
        {

#line 136
            break;
        }
        float u_0 = t_1 * float(i_0) / 16.0f;
        float cur_0 = length(d0_1 * float2((1.0f - u_0))  + _S16 * float2(u_0) );
        float total_1 = total_0 + (prev_0 + cur_0) * 0.5f * _S17;

#line 136
        int _S18 = i_0 + int(1);

#line 136
        prev_0 = cur_0;

#line 136
        i_0 = _S18;

#line 136
        total_0 = total_1;

#line 136
    }

#line 143
    return total_0;
}


#line 61
float sdLineCapped_0(float2 p_4, float2 a_2, float2 b_2, float hw_0, uint cap_0)
{
    if(cap_0 == 0U)
    {

#line 63
        return sdSegment_0(p_4, a_2, b_2) - hw_0;
    }

#line 64
    float2 ba_1 = b_2 - a_2;
    float _S19 = max(length(ba_1), 9.99999997475242708e-07f);
    float2 dir_0 = ba_1 / float2(_S19) ;
    float2 rel_0 = p_4 - (a_2 + b_2) * float2(0.5f) ;
    float along_0 = abs(dot(rel_0, dir_0));
    float perp_0 = abs(dot(rel_0, float2(- dir_0.y, dir_0.x)));

#line 69
    float capExt_0;
    if(cap_0 == 2U)
    {

#line 70
        capExt_0 = hw_0;

#line 70
    }
    else
    {

#line 70
        capExt_0 = 0.0f;

#line 70
    }
    float _S20 = along_0 - (_S19 * 0.5f + capExt_0);

#line 71
    float _S21 = perp_0 - hw_0;
    return length(max(float2(_S20, _S21), float2(0.0f) )) + min(max(_S20, _S21), 0.0f);
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
    float4 shapeData_0 [[user(SHAPE_DATA)]];
    float4 shapeData2_0 [[user(SHAPE_DATA_2)]];
    [[flat]] float halfWidth_0 [[user(HALF_WIDTH)]];
    [[flat]] uint color_0 [[user(COLOR)]];
    [[flat]] uint shapeType_0 [[user(SHAPE_TYPE)]];
    [[flat]] uint clipIndex_0 [[user(CLIP_INDEX)]];
    [[flat]] uint color2_0 [[user(COLOR_2)]];
    [[flat]] uint flags_0 [[user(FLAGS)]];
};


#line 147 "canvas_shape.slang"
[[fragment]] pixelOutput_0 fragmentMain(pixelInput_0 _S22 [[stage_in]], float4 position_0 [[position]], GlobalsData_natural_0 constant* Globals_1 [[buffer(0)]], ClipRectsData_0 constant* ClipRects_1 [[buffer(1)]])
{

#line 147
    thread KernelContext_0 kernelContext_3;

#line 147
    (&kernelContext_3)->Globals_0 = Globals_1;

#line 147
    (&kernelContext_3)->ClipRects_0 = ClipRects_1;

#line 147
    float _S23 = clipCoverage_0(_S22.pixelPos_1, _S22.clipIndex_0, &kernelContext_3);

#line 147
    bool _S24 = outsideClipRect_0(_S22.pixelPos_1, _S22.clipIndex_0, &kernelContext_3);


    if(_S24)
    {

#line 150
        discard_fragment();

#line 150
    }
    if(_S23 <= 0.0f)
    {

#line 151
        discard_fragment();

#line 151
    }



    bool _S25 = (_S22.shapeType_0) == 3U;

#line 155
    float d_0;

#line 155
    float bezierT_0;

#line 155
    if(_S25)
    {

        float2 _S26 = _S22.shapeData_0.xy;

#line 158
        float2 _S27 = _S22.shapeData_0.zw;

#line 158
        float2 _S28 = _S22.shapeData2_0.xy;

#line 158
        float2 bz_0 = sdBezier_0(_S22.pixelPos_1, _S26, _S27, _S28);
        float _S29 = bz_0.x - _S22.halfWidth_0;
        float bezierT_1 = bz_0.y;
        if(((_S22.flags_0) & 4U) != 0U)
        {

            float _S30 = _S22.shapeData2_0.z;

#line 164
            float period_0 = _S30 + _S22.shapeData2_0.w;
            if(period_0 > 0.0f)
            {
                float s_0 = bezierArcLength_0(_S26, _S27, _S28, bezierT_1);
                if((s_0 - period_0 * floor(s_0 / period_0)) > _S30)
                {

#line 168
                    discard_fragment();

#line 168
                }

#line 165
            }

#line 161
        }

#line 161
        d_0 = _S29;

#line 161
        bezierT_0 = bezierT_1;

#line 155
    }
    else
    {

#line 172
        if((_S22.shapeType_0) == 2U)
        {

            float2 _S31 = _S22.shapeData_0.xy;

#line 175
            float2 _S32 = _S22.shapeData_0.zw;

#line 175
            float _S33 = sdLineCapped_0(_S22.pixelPos_1, _S31, _S32, _S22.halfWidth_0, (_S22.flags_0) & 3U);
            if(((_S22.flags_0) & 4U) != 0U)
            {

                float2 ba_2 = _S32 - _S31;

                float s_1 = dot(_S22.pixelPos_1 - _S31, ba_2 / float2(max(length(ba_2), 9.99999997475242708e-07f)) );
                float _S34 = _S22.shapeData2_0.z;

#line 182
                float period_1 = _S34 + _S22.shapeData2_0.w;

#line 182
                bool _S35;
                if(period_1 > 0.0f)
                {

#line 183
                    _S35 = (s_1 - period_1 * floor(s_1 / period_1)) > _S34;

#line 183
                }
                else
                {

#line 183
                    _S35 = false;

#line 183
                }

#line 183
                if(_S35)
                {

#line 183
                    discard_fragment();

#line 183
                }

#line 176
            }

#line 176
            d_0 = _S33;

#line 172
        }
        else
        {

#line 186
            if((_S22.shapeType_0) == 1U)
            {

#line 186
                d_0 = abs(length(_S22.pixelPos_1 - _S22.shapeData_0.xy) - _S22.shapeData_0.z) - _S22.halfWidth_0;

#line 186
            }
            else
            {

#line 186
                d_0 = length(_S22.pixelPos_1 - _S22.shapeData_0.xy) - _S22.shapeData_0.z;

#line 186
            }

#line 172
        }

#line 172
        bezierT_0 = 0.0f;

#line 155
    }

#line 199
    float coverage_0 = clamp(0.5f - d_0 / max((fwidth((d_0))), 9.99999997475242708e-07f), 0.0f, 1.0f);
    if(coverage_0 <= 0.0f)
    {

#line 200
        discard_fragment();

#line 200
    }

    float4 rgba_0 = unpackARGB_0(_S22.color_0);

#line 202
    float4 rgba_1;
    if(((_S22.flags_0) & 8U) != 0U)
    {

#line 203
        float g_0;



        if(_S25)
        {

#line 207
            g_0 = bezierT_0;

#line 207
        }
        else
        {



            float2 _S36 = _S22.shapeData_0.xy;

#line 213
            float2 ba_3 = _S22.shapeData_0.zw - _S36;

#line 213
            g_0 = clamp(dot(_S22.pixelPos_1 - _S36, ba_3) / max(dot(ba_3, ba_3), 9.99999997475242708e-07f), 0.0f, 1.0f);

#line 207
        }

#line 207
        rgba_1 = mix(rgba_0, unpackARGB_0(_S22.color2_0), float4(g_0) );

#line 203
    }
    else
    {

#line 203
        rgba_1 = rgba_0;

#line 203
    }

#line 203
    pixelOutput_0 _S37 = { float4(rgba_1.xyz, rgba_1.w * coverage_0 * _S23) };

#line 218
    return _S37;
}


#line 218
struct vertexMain_Result_0
{
    float4 position_1 [[position]];
    float2 pixelPos_2 [[user(PIXEL_POS)]];
    float4 shapeData_1 [[user(SHAPE_DATA)]];
    float4 shapeData2_1 [[user(SHAPE_DATA_2)]];
    float halfWidth_1 [[user(HALF_WIDTH)]];
    uint color_1 [[user(COLOR)]];
    uint shapeType_1 [[user(SHAPE_TYPE)]];
    uint clipIndex_1 [[user(CLIP_INDEX)]];
    uint color2_1 [[user(COLOR_2)]];
    uint flags_1 [[user(FLAGS)]];
};


#line 218
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


#line 19
struct Varyings_0
{
    float4 position_2;
    float2 pixelPos_3;
    float4 shapeData_3;
    float4 shapeData2_3;
    [[flat]] float halfWidth_3;
    [[flat]] uint color_3;
    [[flat]] uint shapeType_3;
    [[flat]] uint clipIndex_3;
    [[flat]] uint color2_3;
    [[flat]] uint flags_3;
};


#line 19
[[vertex]] vertexMain_Result_0 vertexMain(vertexInput_0 _S38 [[stage_in]], GlobalsData_natural_0 constant* Globals_2 [[buffer(0)]], ClipRectsData_0 constant* ClipRects_2 [[buffer(1)]])
{

#line 19
    thread KernelContext_0 kernelContext_4;

#line 19
    (&kernelContext_4)->Globals_0 = Globals_2;

#line 19
    (&kernelContext_4)->ClipRects_0 = ClipRects_2;

#line 36
    float2 pixelPos_4 = _S38.outerRect_0.xy + _S38.unitPos_0 * _S38.outerRect_0.zw;

    thread Varyings_0 o_0;

#line 38
    float4 _S39 = project_0(pixelPos_4, &kernelContext_4);
    (&o_0)->position_2 = _S39;
    (&o_0)->pixelPos_3 = pixelPos_4;
    (&o_0)->shapeData_3 = _S38.shapeData_2;
    (&o_0)->shapeData2_3 = _S38.shapeData2_2;
    (&o_0)->halfWidth_3 = _S38.halfWidth_2;
    (&o_0)->color_3 = _S38.color_2;
    (&o_0)->shapeType_3 = _S38.shapeType_2;
    (&o_0)->clipIndex_3 = _S38.clipIndex_2;
    (&o_0)->color2_3 = _S38.color2_2;
    (&o_0)->flags_3 = _S38.flags_2;

#line 48
    thread vertexMain_Result_0 _S40;

#line 48
    (&_S40)->position_1 = o_0.position_2;

#line 48
    (&_S40)->pixelPos_2 = o_0.pixelPos_3;

#line 48
    (&_S40)->shapeData_1 = o_0.shapeData_3;

#line 48
    (&_S40)->shapeData2_1 = o_0.shapeData2_3;

#line 48
    (&_S40)->halfWidth_1 = o_0.halfWidth_3;

#line 48
    (&_S40)->color_1 = o_0.color_3;

#line 48
    (&_S40)->shapeType_1 = o_0.shapeType_3;

#line 48
    (&_S40)->clipIndex_1 = o_0.clipIndex_3;

#line 48
    (&_S40)->color2_1 = o_0.color2_3;

#line 48
    (&_S40)->flags_1 = o_0.flags_3;

#line 48
    return _S40;
}

