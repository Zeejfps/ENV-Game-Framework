// Generated from ZGF.Gui.Shaders/canvas_rect.slang by ZGF.Gui.Shaders/regenerate.cs. Edit the .slang and regenerate.
#version 450
layout(row_major) uniform;
layout(row_major) buffer;

#line 4 0
struct GlobalsData_0
{
    mat4x4 projection_0;
    vec4 canvasMetrics_0;
};


layout(binding = 0)
layout(std140) uniform block_GlobalsData_0
{
    mat4x4 projection_0;
    vec4 canvasMetrics_0;
}Globals_0;

#line 13
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


vec2 fragmentPixelPos_0(vec4 fragCoord_0)
{



    return fragCoord_0.xy * (Globals_0.canvasMetrics_0.xy / Globals_0.canvasMetrics_0.zw);
}


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


#line 39
vec4 unpackARGB_0(uint c_0)
{

#line 45
    return vec4(float((c_0 >> 16) & 255U) / 255.0, float((c_0 >> 8) & 255U) / 255.0, float(c_0 & 255U) / 255.0, float((c_0 >> 24) & 255U) / 255.0);
}


#line 45
layout(location = 0)
out vec4 entryPointParam_fragmentMain_0;


#line 45
flat layout(location = 0)
in vec2 v_rectOrigin_0;


#line 45
flat layout(location = 1)
in vec4 v_rectSize_0;


#line 45
flat layout(location = 2)
in vec4 v_borderRadius_0;


#line 45
flat layout(location = 3)
in vec4 v_borderSize_0;


#line 45
flat layout(location = 4)
in uint v_bgColor_0;


#line 45
flat layout(location = 5)
in uint v_borderColorTop_0;


#line 45
flat layout(location = 6)
in uint v_borderColorRight_0;


#line 45
flat layout(location = 7)
in uint v_borderColorBottom_0;


#line 45
flat layout(location = 8)
in uint v_borderColorLeft_0;


#line 45
flat layout(location = 9)
in uint v_clipIndex_0;


#line 45
flat layout(location = 10)
in float v_fadesEdges_0;


#line 67 1
void main()
{



    vec4 metrics_0 = Globals_0.canvasMetrics_0;
    vec2 pixelPos_0 = fragmentPixelPos_0(gl_FragCoord);
    vec2 localPos_0 = pixelPos_0 - v_rectOrigin_0;
    float clipCover_0 = clipCoverage_0(pixelPos_0, v_clipIndex_0);

#line 75
    bool clippedOut_0;


    if(outsideClipRect_0(pixelPos_0, v_clipIndex_0))
    {

#line 78
        clippedOut_0 = true;

#line 78
    }
    else
    {

#line 78
        clippedOut_0 = clipCover_0 <= 0.0;

#line 78
    }

    float rectW_0 = v_rectSize_0.x;
    float rectH_0 = v_rectSize_0.y;
    float halfW_0 = rectW_0 * 0.5;
    float halfH_0 = rectH_0 * 0.5;

    vec2 _S6 = vec2(halfW_0, halfH_0);

#line 85
    vec2 mirror_0 = abs(localPos_0 - _S6);



    float _S7 = localPos_0.x;

#line 89
    bool right_0 = _S7 > halfW_0;
    float _S8 = localPos_0.y;

#line 90
    bool top_0 = _S8 > halfH_0;

#line 90
    float radius_0;

    if(top_0)
    {

#line 92
        if(right_0)
        {

#line 92
            radius_0 = v_borderRadius_0.y;

#line 92
        }
        else
        {

#line 92
            radius_0 = v_borderRadius_0.x;

#line 92
        }

#line 92
    }
    else
    {

#line 93
        if(right_0)
        {

#line 93
            radius_0 = v_borderRadius_0.z;

#line 93
        }
        else
        {

#line 93
            radius_0 = v_borderRadius_0.w;

#line 93
        }

#line 92
    }

#line 92
    float borderW_0;



    if(right_0)
    {

#line 96
        borderW_0 = v_borderSize_0.y;

#line 96
    }
    else
    {

#line 96
        borderW_0 = v_borderSize_0.w;

#line 96
    }

#line 96
    float borderH_0;
    if(top_0)
    {

#line 97
        borderH_0 = v_borderSize_0.x;

#line 97
    }
    else
    {

#line 97
        borderH_0 = v_borderSize_0.z;

#line 97
    }

    float _S9 = halfW_0 - radius_0;

#line 99
    float _S10 = halfH_0 - radius_0;

#line 99
    vec2 pivot_0 = vec2(_S9, _S10);
    float _S11 = mirror_0.x;

#line 100
    bool inCornerZone_0;

#line 100
    if(_S11 > _S9)
    {

#line 100
        inCornerZone_0 = (mirror_0.y) > _S10;

#line 100
    }
    else
    {

#line 100
        inCornerZone_0 = false;

#line 100
    }

#line 106
    vec2 _S12 = metrics_0.xy;

#line 106
    vec2 _S13 = metrics_0.zw;

#line 106
    vec2 logicalPerDevice_0 = _S12 / _S13;
    vec2 fromPivot_0 = mirror_0 - pivot_0;
    float d_0 = length(fromPivot_0);
    float _S14 = max(dot(abs(fromPivot_0), logicalPerDevice_0) / max(d_0, 9.99999997475242708e-07), 9.99999997475242708e-07);


    float _S15 = max(radius_0 - borderW_0, 9.99999997475242708e-07);
    float _S16 = max(radius_0 - borderH_0, 9.99999997475242708e-07);
    float ix_0 = (_S11 - _S9) / _S15;
    float iy_0 = (mirror_0.y - _S10) / _S16;
    float e_0 = ix_0 * ix_0 + iy_0 * iy_0;
    float _S17 = max(dot(abs(vec2(2.0 * ix_0 / _S15, 2.0 * iy_0 / _S16)), logicalPerDevice_0), 9.99999997475242708e-07);

    if(clippedOut_0)
    {

#line 119
        discard;

#line 119
    }

#line 119
    bool inBottom_0;



    if((v_borderSize_0.z) > 0.0)
    {

#line 123
        inBottom_0 = _S8 < (v_borderSize_0.z);

#line 123
    }
    else
    {

#line 123
        inBottom_0 = false;

#line 123
    }

#line 123
    bool inTop_0;
    if((v_borderSize_0.x) > 0.0)
    {

#line 124
        inTop_0 = _S8 >= (rectH_0 - v_borderSize_0.x);

#line 124
    }
    else
    {

#line 124
        inTop_0 = false;

#line 124
    }

#line 124
    bool inRight_0;
    if((v_borderSize_0.y) > 0.0)
    {

#line 125
        inRight_0 = _S7 >= (rectW_0 - v_borderSize_0.y);

#line 125
    }
    else
    {

#line 125
        inRight_0 = false;

#line 125
    }

#line 125
    bool inLeft_0;
    if((v_borderSize_0.w) > 0.0)
    {

#line 126
        inLeft_0 = _S7 < (v_borderSize_0.w);

#line 126
    }
    else
    {

#line 126
        inLeft_0 = false;

#line 126
    }



    if(inCornerZone_0)
    {

#line 130
        clippedOut_0 = radius_0 > 0.0;

#line 130
    }
    else
    {

#line 130
        clippedOut_0 = false;

#line 130
    }

#line 130
    float fillFactor_0;

#line 130
    float coverage_0;

#line 130
    if(clippedOut_0)
    {

        float coverage_1 = clamp((radius_0 - d_0) / _S14 + 0.5, 0.0, 1.0);
        if(coverage_1 <= 0.0)
        {

#line 134
            discard;

#line 134
        }
        if(borderW_0 < radius_0)
        {

#line 135
            clippedOut_0 = borderH_0 < radius_0;

#line 135
        }
        else
        {

#line 135
            clippedOut_0 = false;

#line 135
        }

#line 135
        if(clippedOut_0)
        {

#line 135
            fillFactor_0 = clamp((1.0 - e_0) / _S17 + 0.5, 0.0, 1.0);

#line 135
        }
        else
        {

#line 135
            fillFactor_0 = 0.0;

#line 135
        }

#line 135
        coverage_0 = coverage_1;

#line 130
    }
    else
    {

#line 147
        if(v_fadesEdges_0 > 0.0)
        {



            vec2 devicePerLogical_0 = _S13 / _S12;
            vec2 inside_0 = (_S6 - mirror_0) * devicePerLogical_0;
            float coverage_2 = clamp(inside_0.x + 0.5, 0.0, 1.0) * clamp(inside_0.y + 0.5, 0.0, 1.0);
            if(coverage_2 <= 0.0)
            {

#line 155
                discard;

#line 155
            }

            if((v_borderSize_0.w) > 0.0)
            {

#line 157
                fillFactor_0 = _S7 - v_borderSize_0.w;

#line 157
            }
            else
            {

#line 157
                fillFactor_0 = 1.0e+06;

#line 157
            }
            if((v_borderSize_0.y) > 0.0)
            {

#line 158
                coverage_0 = rectW_0 - v_borderSize_0.y - _S7;

#line 158
            }
            else
            {

#line 158
                coverage_0 = 1.0e+06;

#line 158
            }

#line 158
            float fromBottom_0;
            if((v_borderSize_0.z) > 0.0)
            {

#line 159
                fromBottom_0 = _S8 - v_borderSize_0.z;

#line 159
            }
            else
            {

#line 159
                fromBottom_0 = 1.0e+06;

#line 159
            }

#line 159
            float fromTop_0;
            if((v_borderSize_0.x) > 0.0)
            {

#line 160
                fromTop_0 = rectH_0 - v_borderSize_0.x - _S8;

#line 160
            }
            else
            {

#line 160
                fromTop_0 = 1.0e+06;

#line 160
            }
            float _S18 = min(fillFactor_0, coverage_0);

#line 161
            float _S19 = min(fromBottom_0, fromTop_0);

#line 161
            vec2 intoFill_0 = vec2(_S18, _S19) * devicePerLogical_0;
            float _S20 = clamp(intoFill_0.x + 0.5, 0.0, 1.0) * clamp(intoFill_0.y + 0.5, 0.0, 1.0);

            if(inBottom_0)
            {

#line 164
                clippedOut_0 = true;

#line 164
            }
            else
            {

#line 164
                clippedOut_0 = inTop_0;

#line 164
            }

#line 164
            if(clippedOut_0)
            {

#line 164
                clippedOut_0 = true;

#line 164
            }
            else
            {

#line 164
                clippedOut_0 = inRight_0;

#line 164
            }

#line 164
            if(clippedOut_0)
            {

#line 164
                clippedOut_0 = true;

#line 164
            }
            else
            {

#line 164
                clippedOut_0 = inLeft_0;

#line 164
            }

#line 164
            if(!clippedOut_0)
            {
                float _S21 = min(_S18, _S19);
                if(_S21 == fromBottom_0)
                {

#line 167
                    inBottom_0 = (v_borderSize_0.z) > 0.0;

#line 167
                }
                else
                {

#line 167
                    inBottom_0 = false;

#line 167
                }
                bool _S22 = !inBottom_0;

#line 168
                if(_S22)
                {

#line 168
                    clippedOut_0 = _S21 == fromTop_0;

#line 168
                }
                else
                {

#line 168
                    clippedOut_0 = false;

#line 168
                }

#line 168
                if(clippedOut_0)
                {

#line 168
                    inTop_0 = (v_borderSize_0.x) > 0.0;

#line 168
                }
                else
                {

#line 168
                    inTop_0 = false;

#line 168
                }
                if(_S22)
                {

#line 169
                    clippedOut_0 = !inTop_0;

#line 169
                }
                else
                {

#line 169
                    clippedOut_0 = false;

#line 169
                }

#line 169
                if(clippedOut_0)
                {

#line 169
                    clippedOut_0 = _S21 == coverage_0;

#line 169
                }
                else
                {

#line 169
                    clippedOut_0 = false;

#line 169
                }

#line 169
                if(clippedOut_0)
                {

#line 169
                    inRight_0 = (v_borderSize_0.y) > 0.0;

#line 169
                }
                else
                {

#line 169
                    inRight_0 = false;

#line 169
                }
                if(_S22)
                {

#line 170
                    clippedOut_0 = !inTop_0;

#line 170
                }
                else
                {

#line 170
                    clippedOut_0 = false;

#line 170
                }

#line 170
                if(clippedOut_0)
                {

#line 170
                    clippedOut_0 = !inRight_0;

#line 170
                }
                else
                {

#line 170
                    clippedOut_0 = false;

#line 170
                }

#line 170
                if(clippedOut_0)
                {

#line 170
                    clippedOut_0 = _S21 == fillFactor_0;

#line 170
                }
                else
                {

#line 170
                    clippedOut_0 = false;

#line 170
                }

#line 170
                if(clippedOut_0)
                {

#line 170
                    clippedOut_0 = (v_borderSize_0.w) > 0.0;

#line 170
                }
                else
                {

#line 170
                    clippedOut_0 = false;

#line 170
                }

#line 170
                inLeft_0 = clippedOut_0;

#line 164
            }
            else
            {

#line 164
            }

#line 164
            fillFactor_0 = _S20;

#line 164
            coverage_0 = coverage_2;

#line 147
        }
        else
        {

#line 176
            if(inBottom_0)
            {

#line 176
                clippedOut_0 = true;

#line 176
            }
            else
            {

#line 176
                clippedOut_0 = inTop_0;

#line 176
            }

#line 176
            if(clippedOut_0)
            {

#line 176
                clippedOut_0 = true;

#line 176
            }
            else
            {

#line 176
                clippedOut_0 = inRight_0;

#line 176
            }

#line 176
            if(clippedOut_0)
            {

#line 176
                clippedOut_0 = true;

#line 176
            }
            else
            {

#line 176
                clippedOut_0 = inLeft_0;

#line 176
            }

#line 176
            if(clippedOut_0)
            {

#line 176
                radius_0 = 0.0;

#line 176
            }
            else
            {

#line 176
                radius_0 = 1.0;

#line 176
            }

#line 176
            fillFactor_0 = radius_0;

#line 176
            coverage_0 = 1.0;

#line 147
        }

#line 130
    }

#line 130
    uint pickedColor_0;

#line 181
    if(inBottom_0)
    {

#line 181
        pickedColor_0 = v_borderColorBottom_0;

#line 181
    }
    else
    {

#line 182
        if(inTop_0)
        {

#line 182
            pickedColor_0 = v_borderColorTop_0;

#line 182
        }
        else
        {

#line 183
            if(inRight_0)
            {

#line 183
                pickedColor_0 = v_borderColorRight_0;

#line 183
            }
            else
            {

#line 184
                if(inLeft_0)
                {

#line 184
                    pickedColor_0 = v_borderColorLeft_0;

#line 184
                }
                else
                {

#line 185
                    if(top_0)
                    {

#line 185
                        pickedColor_0 = v_borderColorTop_0;

#line 185
                    }
                    else
                    {

#line 185
                        pickedColor_0 = v_borderColorBottom_0;

#line 185
                    }

#line 184
                }

#line 183
            }

#line 182
        }

#line 181
    }

#line 190
    vec4 fillCol_0 = unpackARGB_0(v_bgColor_0);
    vec4 borderCol_0 = unpackARGB_0(pickedColor_0);
    float _S23 = borderCol_0.w;

#line 192
    float _S24 = fillCol_0.w;

#line 192
    float outA_0 = mix(_S23, _S24, fillFactor_0);
    vec3 outPre_0 = mix(borderCol_0.xyz * _S23, fillCol_0.xyz * _S24, vec3(fillFactor_0));

#line 193
    vec3 outRgb_0;
    if(outA_0 > 0.0)
    {

#line 194
        outRgb_0 = outPre_0 / outA_0;

#line 194
    }
    else
    {

#line 194
        outRgb_0 = vec3(0.0);

#line 194
    }

#line 194
    entryPointParam_fragmentMain_0 = vec4(outRgb_0, outA_0 * coverage_0 * clipCover_0);

#line 194
    return;
}

