// Generated from ZGF.Gui.Shaders/canvas_shape.slang by ZGF.Gui.Shaders/regenerate.cs. Edit the .slang and regenerate.
#version 450
layout(row_major) uniform;
layout(row_major) buffer;

#line 13 0
struct ClipRectsData_0
{
    vec4  rects_0[256];
    vec4  rounds_0[256];
    vec4  radii_0[256];
};

layout(binding = 1)
layout(std140) uniform block_ClipRectsData_0
{
    vec4  rects_0[256];
    vec4  rounds_0[256];
    vec4  radii_0[256];
}ClipRects_0;

#line 50
float clipCoverage_0(vec2 p_0, uint index_0)
{
    float _S1 = max((fwidth((p_0.x))), 9.9999997473787516e-05);
    float r_0 = ClipRects_0.radii_0[index_0].x;
    if(r_0 <= 0.0)
    {

#line 54
        return 1.0;
    }
    vec2 _S2 = ClipRects_0.rounds_0[index_0].zw;

#line 56
    vec2 _S3 = ClipRects_0.rounds_0[index_0].xy;
    vec2 q_0 = abs(p_0 - (_S3 + _S2) * 0.5) - ((_S2 - _S3) * 0.5 - vec2(r_0));

    return clamp(0.5 - (length(max(q_0, vec2(0.0))) + min(max(q_0.x, q_0.y), 0.0) - r_0) / _S1, 0.0, 1.0);
}


bool outsideClipRect_0(vec2 p_1, uint index_1)
{
    vec4 clip_0 = ClipRects_0.rects_0[index_1];
    float _S4 = p_1.x;

#line 66
    bool _S5;

#line 66
    if(_S4 < (ClipRects_0.rects_0[index_1].x))
    {

#line 66
        _S5 = true;

#line 66
    }
    else
    {

#line 66
        _S5 = _S4 >= (clip_0.z);

#line 66
    }

#line 66
    if(_S5)
    {

#line 66
        _S5 = true;

#line 66
    }
    else
    {

#line 66
        _S5 = (p_1.y) < (clip_0.y);

#line 66
    }

#line 66
    if(_S5)
    {

#line 66
        _S5 = true;

#line 66
    }
    else
    {

#line 66
        _S5 = (p_1.y) >= (clip_0.w);

#line 66
    }

#line 66
    return _S5;
}


#line 52 1
float sdSegment_0(vec2 p_2, vec2 a_0, vec2 b_0)
{
    vec2 pa_0 = p_2 - a_0;
    vec2 ba_0 = b_0 - a_0;

    return length(pa_0 - ba_0 * clamp(dot(pa_0, ba_0) / max(dot(ba_0, ba_0), 9.99999997475242708e-07), 0.0, 1.0));
}


#line 77
vec2 sdBezier_0(vec2 p_3, vec2 A_0, vec2 B_0, vec2 C_0)
{
    vec2 b_1 = A_0 - 2.0 * B_0 + C_0;
    float _S6 = dot(b_1, b_1);

#line 80
    if(_S6 < 9.9999997473787516e-05)
    {

        vec2 ac_0 = C_0 - A_0;

        return vec2(sdSegment_0(p_3, A_0, C_0), clamp(dot(p_3 - A_0, ac_0) / max(dot(ac_0, ac_0), 9.99999997475242708e-07), 0.0, 1.0));
    }
    vec2 a_1 = B_0 - A_0;
    vec2 e_0 = A_0 - p_3;
    float kk_0 = 1.0 / _S6;
    float kx_0 = kk_0 * dot(a_1, b_1);
    float ky_0 = kk_0 * (2.0 * dot(a_1, a_1) + dot(e_0, b_1)) / 3.0;



    float pp_0 = ky_0 - kx_0 * kx_0;

    float q_1 = kx_0 * (2.0 * kx_0 * kx_0 - 3.0 * ky_0) + kk_0 * dot(e_0, a_1);
    float h_0 = q_1 * q_1 + 4.0 * (pp_0 * pp_0 * pp_0);

#line 98
    float res_0;

#line 98
    float bestT_0;
    if(h_0 >= 0.0)
    {
        float h_1 = sqrt(h_0);
        vec2 x_0 = (vec2(h_1, - h_1) - q_1) / 2.0;
        vec2 uv_0 = vec2((ivec2(sign((x_0))))) * pow(abs(x_0), vec2(0.3333333432674408));
        float t_0 = clamp(uv_0.x + uv_0.y - kx_0, 0.0, 1.0);
        vec2 dd_0 = e_0 + (2.0 * a_1 + b_1 * t_0) * t_0;

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
        float v_0 = acos(q_1 / (pp_0 * z_0 * 2.0)) / 3.0;
        float m_0 = cos(v_0);

        float t0_0 = clamp((m_0 + m_0) * z_0 - kx_0, 0.0, 1.0);
        float t1_0 = clamp((- (sin(v_0) * 1.7320507764816284) - m_0) * z_0 - kx_0, 0.0, 1.0);
        vec2 _S7 = 2.0 * a_1;

#line 117
        vec2 d0_0 = e_0 + (_S7 + b_1 * t0_0) * t0_0;
        vec2 d1_0 = e_0 + (_S7 + b_1 * t1_0) * t1_0;
        float dd0_0 = dot(d0_0, d0_0);
        float dd1_0 = dot(d1_0, d1_0);
        float _S8 = min(dd0_0, dd1_0);
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
        float _S9 = res_0;

#line 122
        res_0 = _S8;

#line 122
        bestT_0 = _S9;

#line 99
    }

#line 124
    return vec2(sqrt(res_0), bestT_0);
}


float bezierArcLength_0(vec2 A_1, vec2 B_1, vec2 C_1, float t_1)
{
    vec2 d0_1 = 2.0 * (B_1 - A_1);
    vec2 _S10 = 2.0 * (C_1 - B_1);

    float _S11 = t_1 / 16.0;

#line 133
    float prev_0 = length(d0_1);

#line 133
    int i_0 = 1;

#line 133
    float total_0 = 0.0;


    for(;;)
    {

#line 136
        if(i_0 <= 16)
        {
        }
        else
        {

#line 136
            break;
        }
        float u_0 = t_1 * float(i_0) / 16.0;
        float cur_0 = length(d0_1 * (1.0 - u_0) + _S10 * u_0);
        float total_1 = total_0 + (prev_0 + cur_0) * 0.5 * _S11;

#line 136
        int _S12 = i_0 + 1;

#line 136
        prev_0 = cur_0;

#line 136
        i_0 = _S12;

#line 136
        total_0 = total_1;

#line 136
    }

#line 143
    return total_0;
}


#line 61
float sdLineCapped_0(vec2 p_4, vec2 a_2, vec2 b_2, float hw_0, uint cap_0)
{
    if(cap_0 == 0U)
    {

#line 63
        return sdSegment_0(p_4, a_2, b_2) - hw_0;
    }

#line 64
    vec2 ba_1 = b_2 - a_2;
    float _S13 = max(length(ba_1), 9.99999997475242708e-07);
    vec2 dir_0 = ba_1 / _S13;
    vec2 rel_0 = p_4 - (a_2 + b_2) * 0.5;
    float along_0 = abs(dot(rel_0, dir_0));
    float perp_0 = abs(dot(rel_0, vec2(- dir_0.y, dir_0.x)));

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
        capExt_0 = 0.0;

#line 70
    }
    float _S14 = along_0 - (_S13 * 0.5 + capExt_0);

#line 71
    float _S15 = perp_0 - hw_0;
    return length(max(vec2(_S14, _S15), vec2(0.0))) + min(max(_S14, _S15), 0.0);
}


#line 39 0
vec4 unpackARGB_0(uint c_0)
{

#line 45
    return vec4(float((c_0 >> 16) & 255U) / 255.0, float((c_0 >> 8) & 255U) / 255.0, float(c_0 & 255U) / 255.0, float((c_0 >> 24) & 255U) / 255.0);
}


#line 45
layout(location = 0)
out vec4 entryPointParam_fragmentMain_0;


#line 45
layout(location = 0)
in vec2 v_pixelPos_0;


#line 45
layout(location = 1)
in vec4 v_shapeData_0;


#line 45
layout(location = 2)
in vec4 v_shapeData2_0;


#line 45
flat layout(location = 3)
in float v_halfWidth_0;


#line 45
flat layout(location = 4)
in uint v_color_0;


#line 45
flat layout(location = 5)
in uint v_shapeType_0;


#line 45
flat layout(location = 6)
in uint v_clipIndex_0;


#line 45
flat layout(location = 7)
in uint v_color2_0;


#line 45
flat layout(location = 8)
in uint v_flags_0;


#line 147 1
void main()
{
    float clipCover_0 = clipCoverage_0(v_pixelPos_0, v_clipIndex_0);
    if(outsideClipRect_0(v_pixelPos_0, v_clipIndex_0))
    {

#line 150
        discard;

#line 150
    }
    if(clipCover_0 <= 0.0)
    {

#line 151
        discard;

#line 151
    }

#line 151
    float d_0;

#line 151
    float bezierT_0;



    if(v_shapeType_0 == 3U)
    {

        vec2 bz_0 = sdBezier_0(v_pixelPos_0, v_shapeData_0.xy, v_shapeData_0.zw, v_shapeData2_0.xy);
        float _S16 = bz_0.x - v_halfWidth_0;
        float bezierT_1 = bz_0.y;
        if((v_flags_0 & 4U) != 0U)
        {

            float period_0 = v_shapeData2_0.z + v_shapeData2_0.w;
            if(period_0 > 0.0)
            {
                float s_0 = bezierArcLength_0(v_shapeData_0.xy, v_shapeData_0.zw, v_shapeData2_0.xy, bezierT_1);
                if((s_0 - period_0 * floor(s_0 / period_0)) > (v_shapeData2_0.z))
                {

#line 168
                    discard;

#line 168
                }

#line 165
            }

#line 161
        }

#line 161
        d_0 = _S16;

#line 161
        bezierT_0 = bezierT_1;

#line 155
    }
    else
    {

#line 172
        if(v_shapeType_0 == 2U)
        {

            float _S17 = sdLineCapped_0(v_pixelPos_0, v_shapeData_0.xy, v_shapeData_0.zw, v_halfWidth_0, v_flags_0 & 3U);
            if((v_flags_0 & 4U) != 0U)
            {

                vec2 ba_2 = v_shapeData_0.zw - v_shapeData_0.xy;

                float s_1 = dot(v_pixelPos_0 - v_shapeData_0.xy, ba_2 / max(length(ba_2), 9.99999997475242708e-07));
                float period_1 = v_shapeData2_0.z + v_shapeData2_0.w;

#line 182
                bool _S18;
                if(period_1 > 0.0)
                {

#line 183
                    _S18 = (s_1 - period_1 * floor(s_1 / period_1)) > (v_shapeData2_0.z);

#line 183
                }
                else
                {

#line 183
                    _S18 = false;

#line 183
                }

#line 183
                if(_S18)
                {

#line 183
                    discard;

#line 183
                }

#line 176
            }

#line 176
            d_0 = _S17;

#line 172
        }
        else
        {

#line 186
            if(v_shapeType_0 == 1U)
            {

#line 186
                d_0 = abs(length(v_pixelPos_0 - v_shapeData_0.xy) - v_shapeData_0.z) - v_halfWidth_0;

#line 186
            }
            else
            {

#line 186
                d_0 = length(v_pixelPos_0 - v_shapeData_0.xy) - v_shapeData_0.z;

#line 186
            }

#line 172
        }

#line 172
        bezierT_0 = 0.0;

#line 155
    }

#line 199
    float coverage_0 = clamp(0.5 - d_0 / max((fwidth((d_0))), 9.99999997475242708e-07), 0.0, 1.0);
    if(coverage_0 <= 0.0)
    {

#line 200
        discard;

#line 200
    }

    vec4 rgba_0 = unpackARGB_0(v_color_0);

#line 202
    vec4 rgba_1;
    if((v_flags_0 & 8U) != 0U)
    {

#line 203
        float g_0;



        if(v_shapeType_0 == 3U)
        {

#line 207
            g_0 = bezierT_0;

#line 207
        }
        else
        {



            vec2 ba_3 = v_shapeData_0.zw - v_shapeData_0.xy;

#line 213
            g_0 = clamp(dot(v_pixelPos_0 - v_shapeData_0.xy, ba_3) / max(dot(ba_3, ba_3), 9.99999997475242708e-07), 0.0, 1.0);

#line 207
        }

#line 207
        rgba_1 = mix(rgba_0, unpackARGB_0(v_color2_0), vec4(g_0));

#line 203
    }
    else
    {

#line 203
        rgba_1 = rgba_0;

#line 203
    }

#line 203
    entryPointParam_fragmentMain_0 = vec4(rgba_1.xyz, rgba_1.w * coverage_0 * clipCover_0);

#line 203
    return;
}

