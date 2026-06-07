#version 450
layout(column_major) uniform;
layout(column_major) buffer;

#line 38 0
struct SLANG_ParameterGroup_Globals_0
{
    mat4x4 u_projection_0;
};


#line 38
layout(binding = 0)
layout(std140) uniform block_SLANG_ParameterGroup_Globals_0
{
    mat4x4 u_projection_0;
}Globals_0;
struct SLANG_ParameterGroup_ClipRects_0
{
    vec4  u_clipRects_0[256];
};


#line 43
layout(binding = 1)
layout(std140) uniform block_SLANG_ParameterGroup_ClipRects_0
{
    vec4  u_clipRects_0[256];
}ClipRects_0;

#line 19
layout(location = 0)
out vec2 entryPointParam_vertexMain_pixelPos_0;


#line 19
layout(location = 1)
out vec2 entryPointParam_vertexMain_localPos_0;


#line 19
layout(location = 2)
out vec4 entryPointParam_vertexMain_rectSize_0;


#line 19
layout(location = 3)
out vec4 entryPointParam_vertexMain_borderRadius_0;


#line 19
layout(location = 4)
out vec4 entryPointParam_vertexMain_borderSize_0;


#line 19
flat layout(location = 5)
out uint entryPointParam_vertexMain_bgColor_0;


#line 19
flat layout(location = 6)
out uint entryPointParam_vertexMain_borderColorTop_0;


#line 19
flat layout(location = 7)
out uint entryPointParam_vertexMain_borderColorRight_0;


#line 19
flat layout(location = 8)
out uint entryPointParam_vertexMain_borderColorBottom_0;


#line 19
flat layout(location = 9)
out uint entryPointParam_vertexMain_borderColorLeft_0;


#line 19
flat layout(location = 10)
out uint entryPointParam_vertexMain_clipIndex_0;


#line 19
layout(location = 0)
in vec2 input_unitPos_0;


#line 19
layout(location = 1)
in vec4 input_rect_0;


#line 19
layout(location = 2)
in vec4 input_borderRadius_0;


#line 19
layout(location = 3)
in vec4 input_borderSize_0;


#line 19
layout(location = 4)
in uint input_bgColor_0;


#line 19
layout(location = 5)
in uint input_borderColorTop_0;


#line 19
layout(location = 6)
in uint input_borderColorRight_0;


#line 19
layout(location = 7)
in uint input_borderColorBottom_0;


#line 19
layout(location = 8)
in uint input_borderColorLeft_0;


#line 19
layout(location = 9)
in uint input_clipIndex_0;

struct Varyings_0
{
    vec4 position_0;
    vec2 pixelPos_0;
    vec2 localPos_0;
    vec4 rectSize_0;
    vec4 borderRadius_0;
    vec4 borderSize_0;
    uint bgColor_0;
    uint borderColorTop_0;
    uint borderColorRight_0;
    uint borderColorBottom_0;
    uint borderColorLeft_0;
    uint clipIndex_0;
};


#line 49
void main()
{

    vec2 pixelPos_1 = input_rect_0.xy + input_unitPos_0 * input_rect_0.zw;

#line 51
    Varyings_0 o_0;

    o_0.position_0 = (((vec4(pixelPos_1, 0.0, 1.0)) * (Globals_0.u_projection_0)));
    o_0.pixelPos_0 = pixelPos_1;
    o_0.localPos_0 = input_unitPos_0 * input_rect_0.zw;
    o_0.rectSize_0 = vec4(input_rect_0.zw, input_rect_0.zw);
    o_0.borderRadius_0 = input_borderRadius_0;
    o_0.borderSize_0 = input_borderSize_0;
    o_0.bgColor_0 = input_bgColor_0;
    o_0.borderColorTop_0 = input_borderColorTop_0;
    o_0.borderColorRight_0 = input_borderColorRight_0;
    o_0.borderColorBottom_0 = input_borderColorBottom_0;
    o_0.borderColorLeft_0 = input_borderColorLeft_0;
    o_0.clipIndex_0 = input_clipIndex_0;
    Varyings_0 _S1 = o_0;

#line 65
    gl_Position = o_0.position_0;

#line 65
    entryPointParam_vertexMain_pixelPos_0 = _S1.pixelPos_0;

#line 65
    entryPointParam_vertexMain_localPos_0 = _S1.localPos_0;

#line 65
    entryPointParam_vertexMain_rectSize_0 = _S1.rectSize_0;

#line 65
    entryPointParam_vertexMain_borderRadius_0 = _S1.borderRadius_0;

#line 65
    entryPointParam_vertexMain_borderSize_0 = _S1.borderSize_0;

#line 65
    entryPointParam_vertexMain_bgColor_0 = _S1.bgColor_0;

#line 65
    entryPointParam_vertexMain_borderColorTop_0 = _S1.borderColorTop_0;

#line 65
    entryPointParam_vertexMain_borderColorRight_0 = _S1.borderColorRight_0;

#line 65
    entryPointParam_vertexMain_borderColorBottom_0 = _S1.borderColorBottom_0;

#line 65
    entryPointParam_vertexMain_borderColorLeft_0 = _S1.borderColorLeft_0;

#line 65
    entryPointParam_vertexMain_clipIndex_0 = _S1.clipIndex_0;

#line 65
    return;
}

vec4 unpackARGB_0(uint c_0)
{

#line 74
    return vec4(float((c_0 >> 16) & 255U) / 255.0, float((c_0 >> 8) & 255U) / 255.0, float(c_0 & 255U) / 255.0, float((c_0 >> 24) & 255U) / 255.0);
}


#line 74
layout(location = 0)
out vec4 entryPointParam_fragmentMain_0;


#line 74
layout(location = 0)
in vec2 v_pixelPos_0;


#line 74
layout(location = 1)
in vec2 v_localPos_0;


#line 74
layout(location = 2)
in vec4 v_rectSize_0;


#line 74
layout(location = 3)
in vec4 v_borderRadius_0;


#line 74
layout(location = 4)
in vec4 v_borderSize_0;


#line 74
flat layout(location = 5)
in uint v_bgColor_0;


#line 74
flat layout(location = 6)
in uint v_borderColorTop_0;


#line 74
flat layout(location = 7)
in uint v_borderColorRight_0;


#line 74
flat layout(location = 8)
in uint v_borderColorBottom_0;


#line 74
flat layout(location = 9)
in uint v_borderColorLeft_0;


#line 74
flat layout(location = 10)
in uint v_clipIndex_0;


void main()
{

    vec4 clip_0 = ClipRects_0.u_clipRects_0[v_clipIndex_0];

#line 81
    bool inCornerZone_0;
    if((v_pixelPos_0.x) < (ClipRects_0.u_clipRects_0[v_clipIndex_0].x))
    {

#line 82
        inCornerZone_0 = true;

#line 82
    }
    else
    {

#line 82
        inCornerZone_0 = (v_pixelPos_0.x) >= (clip_0.z);

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
        inCornerZone_0 = (v_pixelPos_0.y) < (clip_0.y);

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
        inCornerZone_0 = (v_pixelPos_0.y) >= (clip_0.w);

#line 83
    }

#line 82
    if(inCornerZone_0)
    {

        discard;

#line 82
    }

#line 88
    float rectW_0 = v_rectSize_0.x;
    float rectH_0 = v_rectSize_0.y;
    float halfW_0 = rectW_0 * 0.5;
    float halfH_0 = rectH_0 * 0.5;

    vec2 mirror_0 = abs(v_localPos_0 - vec2(halfW_0, halfH_0));

    bool right_0 = (v_localPos_0.x) > halfW_0;
    bool top_0 = (v_localPos_0.y) > halfH_0;

#line 96
    float radius_0;

    if(top_0)
    {

#line 98
        if(right_0)
        {

#line 98
            radius_0 = v_borderRadius_0.y;

#line 98
        }
        else
        {

#line 98
            radius_0 = v_borderRadius_0.x;

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
            radius_0 = v_borderRadius_0.z;

#line 99
        }
        else
        {

#line 99
            radius_0 = v_borderRadius_0.w;

#line 99
        }

#line 98
    }

#line 98
    float borderW_0;


    if(right_0)
    {

#line 101
        borderW_0 = v_borderSize_0.y;

#line 101
    }
    else
    {

#line 101
        borderW_0 = v_borderSize_0.w;

#line 101
    }

#line 101
    float borderH_0;
    if(top_0)
    {

#line 102
        borderH_0 = v_borderSize_0.x;

#line 102
    }
    else
    {

#line 102
        borderH_0 = v_borderSize_0.z;

#line 102
    }

    float _S2 = halfW_0 - radius_0;

#line 104
    float _S3 = halfH_0 - radius_0;

#line 104
    vec2 pivot_0 = vec2(_S2, _S3);
    float _S4 = mirror_0.x;

#line 105
    if(_S4 > _S2)
    {

#line 105
        inCornerZone_0 = (mirror_0.y) > _S3;

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
            discard;

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
            float ix_0 = (_S4 - _S2) / max(radius_0 - borderW_0, 9.99999997475242708e-07);
            float iy_0 = (mirror_0.y - _S3) / max(radius_0 - borderH_0, 9.99999997475242708e-07);

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
        if(_S4 < (halfW_0 - borderW_0))
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
        entryPointParam_fragmentMain_0 = unpackARGB_0(v_bgColor_0);

#line 130
        return;
    }

#line 137
    bool inTop_0 = (v_localPos_0.y) >= (rectH_0 - v_borderSize_0.x);
    bool inRight_0 = (v_localPos_0.x) >= (rectW_0 - v_borderSize_0.y);
    bool inLeft_0 = (v_localPos_0.x) < (v_borderSize_0.w);

#line 139
    uint pickedColor_0;


    if((v_localPos_0.y) < (v_borderSize_0.z))
    {

#line 142
        pickedColor_0 = v_borderColorBottom_0;

#line 142
    }
    else
    {

#line 143
        if(inTop_0)
        {

#line 143
            pickedColor_0 = v_borderColorTop_0;

#line 143
        }
        else
        {

#line 144
            if(inRight_0)
            {

#line 144
                pickedColor_0 = v_borderColorRight_0;

#line 144
            }
            else
            {

#line 145
                if(inLeft_0)
                {

#line 145
                    pickedColor_0 = v_borderColorLeft_0;

#line 145
                }
                else
                {
                    if(top_0)
                    {

#line 148
                        pickedColor_0 = v_borderColorTop_0;

#line 148
                    }
                    else
                    {

#line 148
                        pickedColor_0 = v_borderColorBottom_0;

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
    entryPointParam_fragmentMain_0 = unpackARGB_0(pickedColor_0);

#line 142
    return;
}

