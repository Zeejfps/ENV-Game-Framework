#include <metal_stdlib>
#include <metal_math>
#include <metal_texture>
using namespace metal;

#line 68 "G:/Dev/RiderProjects/ENV-Game-Framework/ZGF.Gui.Tests/Assets/Shaders/canvas_rect.slang"
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


#line 38 "G:/Dev/RiderProjects/ENV-Game-Framework/ZGF.Gui.Tests/Assets/Shaders/canvas_rect.slang"
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


    if(inCornerZone_0)
    {

#line 108
        inCornerZone_0 = radius_0 > 0.0;

#line 108
    }
    else
    {

#line 108
        inCornerZone_0 = false;

#line 108
    }

#line 108
    bool isFill_0;

#line 108
    if(inCornerZone_0)
    {

        if((length(mirror_0 - pivot_0)) > radius_0)
        {

#line 111
            discard_fragment();

#line 111
        }
        if(borderH_0 < radius_0)
        {

#line 112
            inCornerZone_0 = borderW_0 < radius_0;

#line 112
        }
        else
        {

#line 112
            inCornerZone_0 = false;

#line 112
        }

#line 112
        if(inCornerZone_0)
        {
            float ix_0 = (_S7 - _S5) / max(radius_0 - borderW_0, 9.99999997475242708e-07);
            float iy_0 = (mirror_0.y - _S6) / max(radius_0 - borderH_0, 9.99999997475242708e-07);

#line 115
            isFill_0 = (ix_0 * ix_0 + iy_0 * iy_0) <= 1.0;

#line 112
        }
        else
        {

#line 112
            isFill_0 = false;

#line 112
        }

#line 108
    }
    else
    {

#line 126
        bool insideY_0 = (mirror_0.y) < (halfH_0 - borderH_0);
        if(_S7 < (halfW_0 - borderW_0))
        {

#line 127
            inCornerZone_0 = insideY_0;

#line 127
        }
        else
        {

#line 127
            inCornerZone_0 = false;

#line 127
        }

#line 127
        isFill_0 = inCornerZone_0;

#line 108
    }

#line 130
    if(isFill_0)
    {

#line 130
        pixelOutput_0 _S8 = { unpackARGB_0(_S1.bgColor_0) };

        return _S8;
    }



    bool inTop_0 = _S4 >= (rectH_0 - _S1.borderSize_0.x);
    bool inRight_0 = _S3 >= (rectW_0 - _S1.borderSize_0.y);
    bool inLeft_0 = _S3 < (_S1.borderSize_0.w);

#line 139
    uint pickedColor_0;


    if(_S4 < (_S1.borderSize_0.z))
    {

#line 142
        pickedColor_0 = _S1.borderColorBottom_0;

#line 142
    }
    else
    {

#line 143
        if(inTop_0)
        {

#line 143
            pickedColor_0 = _S1.borderColorTop_0;

#line 143
        }
        else
        {

#line 144
            if(inRight_0)
            {

#line 144
                pickedColor_0 = _S1.borderColorRight_0;

#line 144
            }
            else
            {

#line 145
                if(inLeft_0)
                {

#line 145
                    pickedColor_0 = _S1.borderColorLeft_0;

#line 145
                }
                else
                {
                    if(top_0)
                    {

#line 148
                        pickedColor_0 = _S1.borderColorTop_0;

#line 148
                    }
                    else
                    {

#line 148
                        pickedColor_0 = _S1.borderColorBottom_0;

#line 148
                    }

#line 145
                }

#line 144
            }

#line 143
        }

#line 142
    }

#line 142
    pixelOutput_0 _S9 = { unpackARGB_0(pickedColor_0) };

#line 152
    return _S9;
}


#line 152
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


#line 152
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
[[vertex]] vertexMain_Result_0 vertexMain(vertexInput_0 _S10 [[stage_in]], SLANG_ParameterGroup_Globals_0 constant* Globals_2 [[buffer(0)]], SLANG_ParameterGroup_ClipRects_0 constant* ClipRects_2 [[buffer(1)]])
{

#line 22
    thread KernelContext_0 kernelContext_1;

#line 22
    (&kernelContext_1)->Globals_0 = Globals_2;

#line 22
    (&kernelContext_1)->ClipRects_0 = ClipRects_2;

#line 52
    float2 _S11 = _S10.rect_0.zw;

#line 52
    float2 _S12 = _S10.unitPos_0 * _S11;

#line 52
    float2 pixelPos_3 = _S10.rect_0.xy + _S12;

#line 51
    thread Varyings_0 o_0;

    (&o_0)->position_2 = (((float4(pixelPos_3, 0.0, 1.0)) * (Globals_2->u_projection_0)));
    (&o_0)->pixelPos_2 = pixelPos_3;
    (&o_0)->localPos_2 = _S12;
    (&o_0)->rectSize_2 = float4(_S11, _S11);
    (&o_0)->borderRadius_3 = _S10.borderRadius_2;
    (&o_0)->borderSize_3 = _S10.borderSize_2;
    (&o_0)->bgColor_3 = _S10.bgColor_2;
    (&o_0)->borderColorTop_3 = _S10.borderColorTop_2;
    (&o_0)->borderColorRight_3 = _S10.borderColorRight_2;
    (&o_0)->borderColorBottom_3 = _S10.borderColorBottom_2;
    (&o_0)->borderColorLeft_3 = _S10.borderColorLeft_2;
    (&o_0)->clipIndex_3 = _S10.clipIndex_2;

#line 64
    thread vertexMain_Result_0 _S13;

#line 64
    (&_S13)->position_1 = o_0.position_2;

#line 64
    (&_S13)->pixelPos_1 = o_0.pixelPos_2;

#line 64
    (&_S13)->localPos_1 = o_0.localPos_2;

#line 64
    (&_S13)->rectSize_1 = o_0.rectSize_2;

#line 64
    (&_S13)->borderRadius_1 = o_0.borderRadius_3;

#line 64
    (&_S13)->borderSize_1 = o_0.borderSize_3;

#line 64
    (&_S13)->bgColor_1 = o_0.bgColor_3;

#line 64
    (&_S13)->borderColorTop_1 = o_0.borderColorTop_3;

#line 64
    (&_S13)->borderColorRight_1 = o_0.borderColorRight_3;

#line 64
    (&_S13)->borderColorBottom_1 = o_0.borderColorBottom_3;

#line 64
    (&_S13)->borderColorLeft_1 = o_0.borderColorLeft_3;

#line 64
    (&_S13)->clipIndex_1 = o_0.clipIndex_3;

#line 64
    return _S13;
}

