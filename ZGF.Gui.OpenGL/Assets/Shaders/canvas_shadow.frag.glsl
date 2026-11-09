// Generated from ZGF.Gui.Shaders/canvas_shadow.slang by ZGF.Gui.Shaders/regenerate.cs. Edit the .slang and regenerate.
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


#line 45 1
vec2 erf2_0(vec2 x_0)
{
    vec2 _S6 = vec2((ivec2(sign((x_0)))));
    vec2 a_0 = abs(x_0);
    vec2 v_0 = 1.0 + (0.27839300036430359 + (0.23038899898529053 + 0.07810799777507782 * (a_0 * a_0)) * a_0) * a_0;
    vec2 v_1 = v_0 * v_0;
    return _S6 - _S6 / (v_1 * v_1);
}


#line 60
float roundedBoxShadowX_0(float x_1, float y_0, float sigma_0, float corner_0, vec2 halfSize_0)
{
    float _S7 = min(halfSize_0.y - corner_0 - abs(y_0), 0.0);
    float curved_0 = halfSize_0.x - corner_0 + sqrt(max(0.0, corner_0 * corner_0 - _S7 * _S7));
    vec2 integral_0 = 0.5 + 0.5 * erf2_0(vec2(x_1 - curved_0, x_1 + curved_0) * (sqrt(0.5) / sigma_0));
    return integral_0.y - integral_0.x;
}


#line 54
float gaussian1D_0(float x_2, float sigma_1)
{

    return exp(- (x_2 * x_2) / (2.0 * sigma_1 * sigma_1)) / (sqrt(6.2831854820251465) * sigma_1);
}


#line 68
float roundedBoxShadow_0(vec2 lower_0, vec2 upper_0, vec2 point_0, float sigma_2, float corner_1)
{

    vec2 halfSize_1 = (upper_0 - lower_0) * 0.5;
    vec2 p_2 = point_0 - (lower_0 + upper_0) * 0.5;

    float _S8 = p_2.y;

#line 74
    float _S9 = halfSize_1.y;

#line 74
    float low_0 = _S8 - _S9;
    float high_0 = _S8 + _S9;
    float start_0 = clamp(-3.0 * sigma_2, low_0, high_0);


    float stepSize_0 = (clamp(3.0 * sigma_2, low_0, high_0) - start_0) / 4.0;
    float _S10 = start_0 + stepSize_0 * 0.5;

#line 80
    int i_0 = 0;

#line 80
    float y_1 = _S10;

#line 80
    float value_0 = 0.0;

    for(;;)
    {

#line 82
        if(i_0 < 4)
        {
        }
        else
        {

#line 82
            break;
        }
        float value_1 = value_0 + roundedBoxShadowX_0(p_2.x, _S8 - y_1, sigma_2, corner_1, halfSize_1) * gaussian1D_0(y_1, sigma_2) * stepSize_0;
        float y_2 = y_1 + stepSize_0;

#line 82
        i_0 = i_0 + 1;

#line 82
        y_1 = y_2;

#line 82
        value_0 = value_1;

#line 82
    }

#line 87
    return value_0;
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
in vec4 v_shadowRect_0;


#line 45
layout(location = 2)
in vec4 v_borderRadius_0;


#line 45
flat layout(location = 3)
in float v_sigma_0;


#line 45
flat layout(location = 4)
in uint v_color_0;


#line 45
flat layout(location = 5)
in uint v_clipIndex_0;


#line 91 1
void main()
{
    float clipCover_0 = clipCoverage_0(v_pixelPos_0, v_clipIndex_0);
    if(outsideClipRect_0(v_pixelPos_0, v_clipIndex_0))
    {

#line 94
        discard;

#line 94
    }
    if(clipCover_0 <= 0.0)
    {

#line 95
        discard;

#line 95
    }

    vec2 lower_1 = v_shadowRect_0.xy;

#line 106
    vec4 rgba_0 = unpackARGB_0(v_color_0);

#line 106
    entryPointParam_fragmentMain_0 = vec4(rgba_0.xyz, rgba_0.w * clamp(roundedBoxShadow_0(lower_1, lower_1 + v_shadowRect_0.zw, v_pixelPos_0, v_sigma_0, min(max(max(v_borderRadius_0.x, v_borderRadius_0.y), max(v_borderRadius_0.z, v_borderRadius_0.w)), min(v_shadowRect_0.z, v_shadowRect_0.w) * 0.5)), 0.0, 1.0) * clipCover_0);

#line 106
    return;
}

