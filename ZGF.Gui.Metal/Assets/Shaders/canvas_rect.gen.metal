#include <metal_stdlib>
#include <metal_math>
#include <metal_texture>
using namespace metal;

#line 68 "/tmp/rectgen/canvas_rect.slang"
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
    float2 localPos_0 [[user(TEXCOORD_1)]];
    float4 rectSize_0 [[user(TEXCOORD_2)]];
    float4 borderRadius_0 [[user(TEXCOORD_3)]];
    float4 borderSize_0 [[user(TEXCOORD_4)]];
    [[flat]] uint bgColor_0 [[user(COLOR)]];
    [[flat]] uint borderColorTop_0 [[user(COLOR_1)]];
    [[flat]] uint borderColorRight_0 [[user(COLOR_2)]];
    [[flat]] uint borderColorBottom_0 [[user(COLOR_3)]];
    [[flat]] uint borderColorLeft_0 [[user(COLOR_4)]];
    [[flat]] uint clipIndex_0 [[user(COLOR_5)]];
};


#line 38 "/tmp/rectgen/canvas_rect.slang"
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


#line 78
[[fragment]] pixelOutput_0 fragmentMain(pixelInput_0 _S1 [[stage_in]], float4 position_0 [[position]], SLANG_ParameterGroup_Globals_0 constant* Globals_1 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_1 [[buffer(1)]])
{

#line 78
    thread KernelContext_0 kernelContext_0;

#line 78
    (&kernelContext_0)->Globals_0 = Globals_1;

#line 78
    (&kernelContext_0)->ClipRects_0 = ClipRects_1;


    float4 clip_0 = ClipRects_1->u_clipRects_0[_S1.clipIndex_0];
    float _S2 = _S1.pixelPos_0.x;

#line 82
    bool inCornerZone_0;

#line 82
    if(_S2 < (ClipRects_1->u_clipRects_0[_S1.clipIndex_0].x))
    {

#line 82
        inCornerZone_0 = true;

#line 82
    }
    else
    {

#line 82
        inCornerZone_0 = _S2 >= (clip_0.z);

#line 82
    }

#line 82
    if(inCornerZone_0)
    {

#line 82
        inCornerZone_0 = true;

#line 82
    }
    else
    {

#line 82
        inCornerZone_0 = (_S1.pixelPos_0.y) < (clip_0.y);

#line 82
    }
    if(inCornerZone_0)
    {

#line 83
        inCornerZone_0 = true;

#line 83
    }
    else
    {

#line 83
        inCornerZone_0 = (_S1.pixelPos_0.y) >= (clip_0.w);

#line 83
    }

#line 82
    if(inCornerZone_0)
    {

        discard_fragment();

#line 82
    }

#line 88
    float rectW_0 = _S1.rectSize_0.x;
    float rectH_0 = _S1.rectSize_0.y;
    float halfW_0 = rectW_0 * 0.5;
    float halfH_0 = rectH_0 * 0.5;

    float2 mirror_0 = abs(_S1.localPos_0 - float2(halfW_0, halfH_0));

    float _S3 = _S1.localPos_0.x;

#line 95
    bool right_0 = _S3 > halfW_0;
    float _S4 = _S1.localPos_0.y;

#line 96
    bool top_0 = _S4 > halfH_0;

#line 96
    float radius_0;

    if(top_0)
    {

#line 98
        if(right_0)
        {

#line 98
            radius_0 = _S1.borderRadius_0.y;

#line 98
        }
        else
        {

#line 98
            radius_0 = _S1.borderRadius_0.x;

#line 98
        }

#line 98
    }
    else
    {

#line 99
        if(right_0)
        {

#line 99
            radius_0 = _S1.borderRadius_0.z;

#line 99
        }
        else
        {

#line 99
            radius_0 = _S1.borderRadius_0.w;

#line 99
        }

#line 98
    }

#line 98
    float borderW_0;


    if(right_0)
    {

#line 101
        borderW_0 = _S1.borderSize_0.y;

#line 101
    }
    else
    {

#line 101
        borderW_0 = _S1.borderSize_0.w;

#line 101
    }

#line 101
    float borderH_0;
    if(top_0)
    {

#line 102
        borderH_0 = _S1.borderSize_0.x;

#line 102
    }
    else
    {

#line 102
        borderH_0 = _S1.borderSize_0.z;

#line 102
    }

    float _S5 = halfW_0 - radius_0;

#line 104
    float _S6 = halfH_0 - radius_0;

#line 104
    float2 pivot_0 = float2(_S5, _S6);
    float _S7 = mirror_0.x;

#line 105
    if(_S7 > _S5)
    {

#line 105
        inCornerZone_0 = (mirror_0.y) > _S6;

#line 105
    }
    else
    {

#line 105
        inCornerZone_0 = false;

#line 105
    }



    float d_0 = length(mirror_0 - pivot_0);
    float _S8 = max((fwidth((d_0))), 9.99999997475242708e-07);



    if(inCornerZone_0)
    {

#line 114
        inCornerZone_0 = radius_0 > 0.0;

#line 114
    }
    else
    {

#line 114
        inCornerZone_0 = false;

#line 114
    }

#line 114
    bool isFill_0;

#line 114
    float coverage_0;

#line 114
    if(inCornerZone_0)
    {

        float coverage_1 = clamp((radius_0 - d_0) / _S8 + 0.5, 0.0, 1.0);
        if(coverage_1 <= 0.0)
        {

#line 118
            discard_fragment();

#line 118
        }
        if(borderH_0 < radius_0)
        {

#line 119
            inCornerZone_0 = borderW_0 < radius_0;

#line 119
        }
        else
        {

#line 119
            inCornerZone_0 = false;

#line 119
        }

#line 119
        if(inCornerZone_0)
        {
            float ix_0 = (_S7 - _S5) / max(radius_0 - borderW_0, 9.99999997475242708e-07);
            float iy_0 = (mirror_0.y - _S6) / max(radius_0 - borderH_0, 9.99999997475242708e-07);

#line 122
            isFill_0 = (ix_0 * ix_0 + iy_0 * iy_0) <= 1.0;

#line 119
        }
        else
        {

#line 119
            isFill_0 = false;

#line 119
        }

#line 119
        coverage_0 = coverage_1;

#line 114
    }
    else
    {

#line 133
        bool insideY_0 = (mirror_0.y) < (halfH_0 - borderH_0);
        if(_S7 < (halfW_0 - borderW_0))
        {

#line 134
            inCornerZone_0 = insideY_0;

#line 134
        }
        else
        {

#line 134
            inCornerZone_0 = false;

#line 134
        }

#line 134
        isFill_0 = inCornerZone_0;

#line 134
        coverage_0 = 1.0;

#line 114
    }

#line 137
    if(isFill_0)
    {
        thread float4 fillColor_0 = unpackARGB_0(_S1.bgColor_0);
        fillColor_0.w = fillColor_0.w * coverage_0;

#line 140
        pixelOutput_0 _S9 = { fillColor_0 };
        return _S9;
    }



    bool inTop_0 = _S4 >= (rectH_0 - _S1.borderSize_0.x);
    bool inRight_0 = _S3 >= (rectW_0 - _S1.borderSize_0.y);
    bool inLeft_0 = _S3 < (_S1.borderSize_0.w);

#line 148
    uint pickedColor_0;


    if(_S4 < (_S1.borderSize_0.z))
    {

#line 151
        pickedColor_0 = _S1.borderColorBottom_0;

#line 151
    }
    else
    {

#line 152
        if(inTop_0)
        {

#line 152
            pickedColor_0 = _S1.borderColorTop_0;

#line 152
        }
        else
        {

#line 153
            if(inRight_0)
            {

#line 153
                pickedColor_0 = _S1.borderColorRight_0;

#line 153
            }
            else
            {

#line 154
                if(inLeft_0)
                {

#line 154
                    pickedColor_0 = _S1.borderColorLeft_0;

#line 154
                }
                else
                {
                    if(top_0)
                    {

#line 157
                        pickedColor_0 = _S1.borderColorTop_0;

#line 157
                    }
                    else
                    {

#line 157
                        pickedColor_0 = _S1.borderColorBottom_0;

#line 157
                    }

#line 154
                }

#line 153
            }

#line 152
        }

#line 151
    }

#line 161
    thread float4 borderColor_0 = unpackARGB_0(pickedColor_0);
    borderColor_0.w = borderColor_0.w * coverage_0;

#line 162
    pixelOutput_0 _S10 = { borderColor_0 };
    return _S10;
}


#line 163
struct vertexMain_Result_0
{
    float4 position_1 [[position]];
    float2 pixelPos_1 [[user(TEXCOORD)]];
    float2 localPos_1 [[user(TEXCOORD_1)]];
    float4 rectSize_1 [[user(TEXCOORD_2)]];
    float4 borderRadius_1 [[user(TEXCOORD_3)]];
    float4 borderSize_1 [[user(TEXCOORD_4)]];
    uint bgColor_1 [[user(COLOR)]];
    uint borderColorTop_1 [[user(COLOR_1)]];
    uint borderColorRight_1 [[user(COLOR_2)]];
    uint borderColorBottom_1 [[user(COLOR_3)]];
    uint borderColorLeft_1 [[user(COLOR_4)]];
    uint clipIndex_1 [[user(COLOR_5)]];
};


#line 163
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


#line 22
struct Varyings_0
{
    float4 position_2;
    float2 pixelPos_2;
    float2 localPos_2;
    float4 rectSize_2;
    float4 borderRadius_3;
    float4 borderSize_3;
    [[flat]] uint bgColor_3;
    [[flat]] uint borderColorTop_3;
    [[flat]] uint borderColorRight_3;
    [[flat]] uint borderColorBottom_3;
    [[flat]] uint borderColorLeft_3;
    [[flat]] uint clipIndex_3;
};


#line 22
[[vertex]] vertexMain_Result_0 vertexMain(vertexInput_0 _S11 [[stage_in]], SLANG_ParameterGroup_Globals_0 constant* Globals_2 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_2 [[buffer(1)]])
{

#line 22
    thread KernelContext_0 kernelContext_1;

#line 22
    (&kernelContext_1)->Globals_0 = Globals_2;

#line 22
    (&kernelContext_1)->ClipRects_0 = ClipRects_2;

#line 52
    float2 _S12 = _S11.rect_0.zw;

#line 52
    float2 _S13 = _S11.unitPos_0 * _S12;

#line 52
    float2 pixelPos_3 = _S11.rect_0.xy + _S13;

#line 51
    thread Varyings_0 o_0;

    (&o_0)->position_2 = (((float4(pixelPos_3, 0.0, 1.0)) * (Globals_2->u_projection_0)));
    (&o_0)->pixelPos_2 = pixelPos_3;
    (&o_0)->localPos_2 = _S13;
    (&o_0)->rectSize_2 = float4(_S12, _S12);
    (&o_0)->borderRadius_3 = _S11.borderRadius_2;
    (&o_0)->borderSize_3 = _S11.borderSize_2;
    (&o_0)->bgColor_3 = _S11.bgColor_2;
    (&o_0)->borderColorTop_3 = _S11.borderColorTop_2;
    (&o_0)->borderColorRight_3 = _S11.borderColorRight_2;
    (&o_0)->borderColorBottom_3 = _S11.borderColorBottom_2;
    (&o_0)->borderColorLeft_3 = _S11.borderColorLeft_2;
    (&o_0)->clipIndex_3 = _S11.clipIndex_2;

#line 64
    thread vertexMain_Result_0 _S14;

#line 64
    (&_S14)->position_1 = o_0.position_2;

#line 64
    (&_S14)->pixelPos_1 = o_0.pixelPos_2;

#line 64
    (&_S14)->localPos_1 = o_0.localPos_2;

#line 64
    (&_S14)->rectSize_1 = o_0.rectSize_2;

#line 64
    (&_S14)->borderRadius_1 = o_0.borderRadius_3;

#line 64
    (&_S14)->borderSize_1 = o_0.borderSize_3;

#line 64
    (&_S14)->bgColor_1 = o_0.bgColor_3;

#line 64
    (&_S14)->borderColorTop_1 = o_0.borderColorTop_3;

#line 64
    (&_S14)->borderColorRight_1 = o_0.borderColorRight_3;

#line 64
    (&_S14)->borderColorBottom_1 = o_0.borderColorBottom_3;

#line 64
    (&_S14)->borderColorLeft_1 = o_0.borderColorLeft_3;

#line 64
    (&_S14)->clipIndex_1 = o_0.clipIndex_3;

#line 64
    return _S14;
}

