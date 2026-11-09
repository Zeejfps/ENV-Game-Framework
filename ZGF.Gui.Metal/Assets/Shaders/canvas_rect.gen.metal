// Generated from ZGF.Gui.Shaders/canvas_rect.slang by ZGF.Gui.Shaders/regenerate.cs. Edit the .slang and regenerate.
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



float2 fragmentPixelPos_0(float4 fragCoord_0, KernelContext_0 thread* kernelContext_1)
{

#line 35
    return float2(fragCoord_0.x, kernelContext_1->Globals_0->canvasMetrics_0.w - fragCoord_0.y) * (kernelContext_1->Globals_0->canvasMetrics_0.xy / kernelContext_1->Globals_0->canvasMetrics_0.zw);
}


#line 50
float clipCoverage_0(float2 p_0, uint index_0, KernelContext_0 thread* kernelContext_2)
{
    float _S1 = max((fwidth((p_0.x))), 9.9999997473787516e-05f);
    float r_0 = kernelContext_2->ClipRects_0->radii_0[index_0].x;
    if(r_0 <= 0.0f)
    {

#line 54
        return 1.0f;
    }
    float2 _S2 = kernelContext_2->ClipRects_0->rounds_0[index_0].zw;

#line 56
    float2 _S3 = kernelContext_2->ClipRects_0->rounds_0[index_0].xy;

#line 56
    float2 _S4 = float2(0.5f) ;
    float2 q_0 = abs(p_0 - (_S3 + _S2) * _S4) - ((_S2 - _S3) * _S4 - float2(r_0) );

    return clamp(0.5f - (length(max(q_0, float2(0.0f) )) + min(max(q_0.x, q_0.y), 0.0f) - r_0) / _S1, 0.0f, 1.0f);
}


bool outsideClipRect_0(float2 p_1, uint index_1, KernelContext_0 thread* kernelContext_3)
{
    float4 clip_0 = kernelContext_3->ClipRects_0->rects_0[index_1];
    float _S5 = p_1.x;

#line 66
    bool _S6;

#line 66
    if(_S5 < (kernelContext_3->ClipRects_0->rects_0[index_1].x))
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


#line 39
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
    [[flat]] float2 rectOrigin_0 [[user(RECT_ORIGIN)]];
    [[flat]] float4 rectSize_0 [[user(RECT_SIZE)]];
    [[flat]] float4 borderRadius_0 [[user(BORDER_RADIUS)]];
    [[flat]] float4 borderSize_0 [[user(BORDER_SIZE)]];
    [[flat]] uint bgColor_0 [[user(BG_COLOR)]];
    [[flat]] uint borderColorTop_0 [[user(BORDER_COLOR_TOP)]];
    [[flat]] uint borderColorRight_0 [[user(BORDER_COLOR_RIGHT)]];
    [[flat]] uint borderColorBottom_0 [[user(BORDER_COLOR_BOTTOM)]];
    [[flat]] uint borderColorLeft_0 [[user(BORDER_COLOR_LEFT)]];
    [[flat]] uint clipIndex_0 [[user(CLIP_INDEX)]];
    [[flat]] float fadesEdges_0 [[user(FADES_EDGES)]];
};


#line 67 "canvas_rect.slang"
[[fragment]] pixelOutput_0 fragmentMain(pixelInput_0 _S7 [[stage_in]], float4 position_0 [[position]], GlobalsData_natural_0 constant* Globals_1 [[buffer(0)]], ClipRectsData_0 constant* ClipRects_1 [[buffer(1)]])
{

#line 67
    thread KernelContext_0 kernelContext_4;

#line 67
    (&kernelContext_4)->Globals_0 = Globals_1;

#line 67
    (&kernelContext_4)->ClipRects_0 = ClipRects_1;

#line 72
    float4 metrics_0 = Globals_1->canvasMetrics_0;

#line 72
    float2 _S8 = fragmentPixelPos_0(position_0, &kernelContext_4);

    float2 localPos_0 = _S8 - _S7.rectOrigin_0;

#line 74
    float _S9 = clipCoverage_0(_S8, _S7.clipIndex_0, &kernelContext_4);

#line 74
    bool _S10 = outsideClipRect_0(_S8, _S7.clipIndex_0, &kernelContext_4);

#line 74
    bool clippedOut_0;



    if(_S10)
    {

#line 78
        clippedOut_0 = true;

#line 78
    }
    else
    {

#line 78
        clippedOut_0 = _S9 <= 0.0f;

#line 78
    }

    float rectW_0 = _S7.rectSize_0.x;
    float rectH_0 = _S7.rectSize_0.y;
    float halfW_0 = rectW_0 * 0.5f;
    float halfH_0 = rectH_0 * 0.5f;

    float2 _S11 = float2(halfW_0, halfH_0);

#line 85
    float2 mirror_0 = abs(localPos_0 - _S11);



    float _S12 = localPos_0.x;

#line 89
    bool right_0 = _S12 > halfW_0;
    float _S13 = localPos_0.y;

#line 90
    bool top_0 = _S13 > halfH_0;

#line 90
    float radius_0;

    if(top_0)
    {

#line 92
        if(right_0)
        {

#line 92
            radius_0 = _S7.borderRadius_0.y;

#line 92
        }
        else
        {

#line 92
            radius_0 = _S7.borderRadius_0.x;

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
            radius_0 = _S7.borderRadius_0.z;

#line 93
        }
        else
        {

#line 93
            radius_0 = _S7.borderRadius_0.w;

#line 93
        }

#line 92
    }

#line 92
    float borderW_0;



    if(right_0)
    {

#line 96
        borderW_0 = _S7.borderSize_0.y;

#line 96
    }
    else
    {

#line 96
        borderW_0 = _S7.borderSize_0.w;

#line 96
    }

#line 96
    float borderH_0;
    if(top_0)
    {

#line 97
        borderH_0 = _S7.borderSize_0.x;

#line 97
    }
    else
    {

#line 97
        borderH_0 = _S7.borderSize_0.z;

#line 97
    }

    float _S14 = halfW_0 - radius_0;

#line 99
    float _S15 = halfH_0 - radius_0;

#line 99
    float2 pivot_0 = float2(_S14, _S15);
    float _S16 = mirror_0.x;

#line 100
    bool inCornerZone_0;

#line 100
    if(_S16 > _S14)
    {

#line 100
        inCornerZone_0 = (mirror_0.y) > _S15;

#line 100
    }
    else
    {

#line 100
        inCornerZone_0 = false;

#line 100
    }

#line 106
    float2 _S17 = metrics_0.xy;

#line 106
    float2 _S18 = metrics_0.zw;

#line 106
    float2 logicalPerDevice_0 = _S17 / _S18;
    float2 fromPivot_0 = mirror_0 - pivot_0;
    float d_0 = length(fromPivot_0);
    float _S19 = max(dot(abs(fromPivot_0), logicalPerDevice_0) / max(d_0, 9.99999997475242708e-07f), 9.99999997475242708e-07f);


    float _S20 = max(radius_0 - borderW_0, 9.99999997475242708e-07f);
    float _S21 = max(radius_0 - borderH_0, 9.99999997475242708e-07f);
    float ix_0 = (_S16 - _S14) / _S20;
    float iy_0 = (mirror_0.y - _S15) / _S21;
    float e_0 = ix_0 * ix_0 + iy_0 * iy_0;
    float _S22 = max(dot(abs(float2(2.0f * ix_0 / _S20, 2.0f * iy_0 / _S21)), logicalPerDevice_0), 9.99999997475242708e-07f);

    if(clippedOut_0)
    {

#line 119
        discard_fragment();

#line 119
    }



    float _S23 = _S7.borderSize_0.z;

#line 123
    bool _S24 = _S23 > 0.0f;

#line 123
    bool inBottom_0;

#line 123
    if(_S24)
    {

#line 123
        inBottom_0 = _S13 < _S23;

#line 123
    }
    else
    {

#line 123
        inBottom_0 = false;

#line 123
    }
    float _S25 = _S7.borderSize_0.x;

#line 124
    bool _S26 = _S25 > 0.0f;

#line 124
    bool inTop_0;

#line 124
    if(_S26)
    {

#line 124
        inTop_0 = _S13 >= (rectH_0 - _S25);

#line 124
    }
    else
    {

#line 124
        inTop_0 = false;

#line 124
    }
    float _S27 = _S7.borderSize_0.y;

#line 125
    bool _S28 = _S27 > 0.0f;

#line 125
    bool inRight_0;

#line 125
    if(_S28)
    {

#line 125
        inRight_0 = _S12 >= (rectW_0 - _S27);

#line 125
    }
    else
    {

#line 125
        inRight_0 = false;

#line 125
    }
    float _S29 = _S7.borderSize_0.w;

#line 126
    bool _S30 = _S29 > 0.0f;

#line 126
    bool inLeft_0;

#line 126
    if(_S30)
    {

#line 126
        inLeft_0 = _S12 < _S29;

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
        clippedOut_0 = radius_0 > 0.0f;

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

        float coverage_1 = clamp((radius_0 - d_0) / _S19 + 0.5f, 0.0f, 1.0f);
        if(coverage_1 <= 0.0f)
        {

#line 134
            discard_fragment();

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
            fillFactor_0 = clamp((1.0f - e_0) / _S22 + 0.5f, 0.0f, 1.0f);

#line 135
        }
        else
        {

#line 135
            fillFactor_0 = 0.0f;

#line 135
        }

#line 135
        coverage_0 = coverage_1;

#line 130
    }
    else
    {

#line 147
        if((_S7.fadesEdges_0) > 0.0f)
        {



            float2 devicePerLogical_0 = _S18 / _S17;
            float2 inside_0 = (_S11 - mirror_0) * devicePerLogical_0;
            float coverage_2 = clamp(inside_0.x + 0.5f, 0.0f, 1.0f) * clamp(inside_0.y + 0.5f, 0.0f, 1.0f);
            if(coverage_2 <= 0.0f)
            {

#line 155
                discard_fragment();

#line 155
            }

            if(_S30)
            {

#line 157
                fillFactor_0 = _S12 - _S29;

#line 157
            }
            else
            {

#line 157
                fillFactor_0 = 1.0e+06f;

#line 157
            }
            if(_S28)
            {

#line 158
                coverage_0 = rectW_0 - _S27 - _S12;

#line 158
            }
            else
            {

#line 158
                coverage_0 = 1.0e+06f;

#line 158
            }

#line 158
            float fromBottom_0;
            if(_S24)
            {

#line 159
                fromBottom_0 = _S13 - _S23;

#line 159
            }
            else
            {

#line 159
                fromBottom_0 = 1.0e+06f;

#line 159
            }

#line 159
            float fromTop_0;
            if(_S26)
            {

#line 160
                fromTop_0 = rectH_0 - _S25 - _S13;

#line 160
            }
            else
            {

#line 160
                fromTop_0 = 1.0e+06f;

#line 160
            }
            float _S31 = min(fillFactor_0, coverage_0);

#line 161
            float _S32 = min(fromBottom_0, fromTop_0);

#line 161
            float2 intoFill_0 = float2(_S31, _S32) * devicePerLogical_0;
            float _S33 = clamp(intoFill_0.x + 0.5f, 0.0f, 1.0f) * clamp(intoFill_0.y + 0.5f, 0.0f, 1.0f);

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
                float _S34 = min(_S31, _S32);
                if(_S34 == fromBottom_0)
                {

#line 167
                    inBottom_0 = _S24;

#line 167
                }
                else
                {

#line 167
                    inBottom_0 = false;

#line 167
                }
                bool _S35 = !inBottom_0;

#line 168
                if(_S35)
                {

#line 168
                    clippedOut_0 = _S34 == fromTop_0;

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
                    inTop_0 = _S26;

#line 168
                }
                else
                {

#line 168
                    inTop_0 = false;

#line 168
                }
                if(_S35)
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
                    clippedOut_0 = _S34 == coverage_0;

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
                    inRight_0 = _S28;

#line 169
                }
                else
                {

#line 169
                    inRight_0 = false;

#line 169
                }
                if(_S35)
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
                    clippedOut_0 = _S34 == fillFactor_0;

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
                    clippedOut_0 = _S30;

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

#line 164
            fillFactor_0 = _S33;

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
                radius_0 = 0.0f;

#line 176
            }
            else
            {

#line 176
                radius_0 = 1.0f;

#line 176
            }

#line 176
            fillFactor_0 = radius_0;

#line 176
            coverage_0 = 1.0f;

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
        pickedColor_0 = _S7.borderColorBottom_0;

#line 181
    }
    else
    {

#line 182
        if(inTop_0)
        {

#line 182
            pickedColor_0 = _S7.borderColorTop_0;

#line 182
        }
        else
        {

#line 183
            if(inRight_0)
            {

#line 183
                pickedColor_0 = _S7.borderColorRight_0;

#line 183
            }
            else
            {

#line 184
                if(inLeft_0)
                {

#line 184
                    pickedColor_0 = _S7.borderColorLeft_0;

#line 184
                }
                else
                {

#line 185
                    if(top_0)
                    {

#line 185
                        pickedColor_0 = _S7.borderColorTop_0;

#line 185
                    }
                    else
                    {

#line 185
                        pickedColor_0 = _S7.borderColorBottom_0;

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
    float4 fillCol_0 = unpackARGB_0(_S7.bgColor_0);
    float4 borderCol_0 = unpackARGB_0(pickedColor_0);
    float _S36 = borderCol_0.w;

#line 192
    float _S37 = fillCol_0.w;

#line 192
    float outA_0 = mix(_S36, _S37, fillFactor_0);
    float3 outPre_0 = mix(borderCol_0.xyz * float3(_S36) , fillCol_0.xyz * float3(_S37) , float3(fillFactor_0) );

#line 193
    float3 outRgb_0;
    if(outA_0 > 0.0f)
    {

#line 194
        outRgb_0 = outPre_0 / float3(outA_0) ;

#line 194
    }
    else
    {

#line 194
        outRgb_0 = float3(0.0f) ;

#line 194
    }

#line 194
    pixelOutput_0 _S38 = { float4(outRgb_0, outA_0 * coverage_0 * _S9) };
    return _S38;
}


#line 195
struct vertexMain_Result_0
{
    float4 position_1 [[position]];
    float2 rectOrigin_1 [[user(RECT_ORIGIN)]];
    float4 rectSize_1 [[user(RECT_SIZE)]];
    float4 borderRadius_1 [[user(BORDER_RADIUS)]];
    float4 borderSize_1 [[user(BORDER_SIZE)]];
    uint bgColor_1 [[user(BG_COLOR)]];
    uint borderColorTop_1 [[user(BORDER_COLOR_TOP)]];
    uint borderColorRight_1 [[user(BORDER_COLOR_RIGHT)]];
    uint borderColorBottom_1 [[user(BORDER_COLOR_BOTTOM)]];
    uint borderColorLeft_1 [[user(BORDER_COLOR_LEFT)]];
    uint clipIndex_1 [[user(CLIP_INDEX)]];
    float fadesEdges_1 [[user(FADES_EDGES)]];
};


#line 195
struct vertexInput_0
{
    float2 unitPos_0 [[attribute(0)]];
    float4 rect_0 [[attribute(1)]];
    float4 borderRadius_2 [[attribute(2)]];
    float4 borderSize_2 [[attribute(3)]];
    uint bgColor_2 [[attribute(4)]];
    uint borderColorTop_2 [[attribute(5)]];
    uint borderColorRight_2 [[attribute(6)]];
    uint borderColorBottom_2 [[attribute(7)]];
    uint borderColorLeft_2 [[attribute(8)]];
    uint clipIndex_2 [[attribute(9)]];
};


#line 17
struct Varyings_0
{
    float4 position_2;
    [[flat]] float2 rectOrigin_2;
    [[flat]] float4 rectSize_2;
    [[flat]] float4 borderRadius_3;
    [[flat]] float4 borderSize_3;
    [[flat]] uint bgColor_3;
    [[flat]] uint borderColorTop_3;
    [[flat]] uint borderColorRight_3;
    [[flat]] uint borderColorBottom_3;
    [[flat]] uint borderColorLeft_3;
    [[flat]] uint clipIndex_3;
    [[flat]] float fadesEdges_2;
};


#line 17
[[vertex]] vertexMain_Result_0 vertexMain(vertexInput_0 _S39 [[stage_in]], GlobalsData_natural_0 constant* Globals_2 [[buffer(0)]], ClipRectsData_0 constant* ClipRects_2 [[buffer(1)]])
{

#line 17
    thread KernelContext_0 kernelContext_5;

#line 17
    (&kernelContext_5)->Globals_0 = Globals_2;

#line 17
    (&kernelContext_5)->ClipRects_0 = ClipRects_2;

#line 41
    float2 devicePerLogical_1 = Globals_2->canvasMetrics_0.zw / Globals_2->canvasMetrics_0.xy;
    float2 _S40 = _S39.rect_0.xy;

#line 42
    float2 low_0 = _S40 * devicePerLogical_1;
    float2 _S41 = _S39.rect_0.zw;

#line 43
    float2 high_0 = (_S40 + _S41) * devicePerLogical_1;
    float2 _S42 = max(abs(low_0 - round(low_0)), abs(high_0 - round(high_0)));

#line 44
    bool hasArea_0;

    if((_S39.rect_0.z) > 0.0f)
    {

#line 46
        hasArea_0 = (_S39.rect_0.w) > 0.0f;

#line 46
    }
    else
    {

#line 46
        hasArea_0 = false;

#line 46
    }

#line 46
    float2 outset_0;
    if(hasArea_0)
    {

#line 47
        outset_0 = step(float2(0.00390625f) , _S42) / devicePerLogical_1;

#line 47
    }
    else
    {

#line 47
        outset_0 = float2(0.0f) ;

#line 47
    }

    thread Varyings_0 o_0;
    if((outset_0.x) > 0.0f)
    {

#line 50
        hasArea_0 = true;

#line 50
    }
    else
    {

#line 50
        hasArea_0 = (outset_0.y) > 0.0f;

#line 50
    }

#line 50
    float _S43;

#line 50
    if(hasArea_0)
    {

#line 50
        _S43 = 1.0f;

#line 50
    }
    else
    {

#line 50
        _S43 = 0.0f;

#line 50
    }

#line 50
    (&o_0)->fadesEdges_2 = _S43;

#line 50
    float4 _S44 = project_0(_S40 - outset_0 + _S39.unitPos_0 * (_S41 + float2(2.0f)  * outset_0), &kernelContext_5);

    (&o_0)->position_2 = _S44;
    (&o_0)->rectOrigin_2 = _S40;
    (&o_0)->rectSize_2 = float4(_S41, _S41);
    (&o_0)->borderRadius_3 = _S39.borderRadius_2;
    (&o_0)->borderSize_3 = _S39.borderSize_2;
    (&o_0)->bgColor_3 = _S39.bgColor_2;
    (&o_0)->borderColorTop_3 = _S39.borderColorTop_2;
    (&o_0)->borderColorRight_3 = _S39.borderColorRight_2;
    (&o_0)->borderColorBottom_3 = _S39.borderColorBottom_2;
    (&o_0)->borderColorLeft_3 = _S39.borderColorLeft_2;
    (&o_0)->clipIndex_3 = _S39.clipIndex_2;
    Varyings_0 _S45 = o_0;

#line 63
    thread vertexMain_Result_0 _S46;

#line 63
    (&_S46)->position_1 = _S45.position_2;

#line 63
    (&_S46)->rectOrigin_1 = _S45.rectOrigin_2;

#line 63
    (&_S46)->rectSize_1 = _S45.rectSize_2;

#line 63
    (&_S46)->borderRadius_1 = _S45.borderRadius_3;

#line 63
    (&_S46)->borderSize_1 = _S45.borderSize_3;

#line 63
    (&_S46)->bgColor_1 = _S45.bgColor_3;

#line 63
    (&_S46)->borderColorTop_1 = _S45.borderColorTop_3;

#line 63
    (&_S46)->borderColorRight_1 = _S45.borderColorRight_3;

#line 63
    (&_S46)->borderColorBottom_1 = _S45.borderColorBottom_3;

#line 63
    (&_S46)->borderColorLeft_1 = _S45.borderColorLeft_3;

#line 63
    (&_S46)->clipIndex_1 = _S45.clipIndex_3;

#line 63
    (&_S46)->fadesEdges_1 = _S45.fadesEdges_2;

#line 63
    return _S46;
}

