#version 450
layout(column_major) uniform;
layout(column_major) buffer;

#line 39 0
struct SLANG_ParameterGroup_Globals_0
{
    mat4x4 u_projection_0;
};


#line 39
layout(binding = 0)
layout(std140) uniform block_SLANG_ParameterGroup_Globals_0
{
    mat4x4 u_projection_0;
}Globals_0;
struct SLANG_ParameterGroup_ClipRects_0
{
    vec4  u_clipRects_0[256];
};


#line 44
layout(binding = 1)
layout(std140) uniform block_SLANG_ParameterGroup_ClipRects_0
{
    vec4  u_clipRects_0[256];
}ClipRects_0;

#line 25
layout(location = 0)
out vec2 entryPointParam_vertexMain_pixelPos_0;


#line 25
layout(location = 1)
out vec4 entryPointParam_vertexMain_shadowRect_0;


#line 25
layout(location = 2)
out vec4 entryPointParam_vertexMain_borderRadius_0;


#line 25
flat layout(location = 3)
out float entryPointParam_vertexMain_sigma_0;


#line 25
flat layout(location = 4)
out uint entryPointParam_vertexMain_color_0;


#line 25
flat layout(location = 5)
out uint entryPointParam_vertexMain_clipIndex_0;


#line 25
layout(location = 0)
in vec2 input_unitPos_0;


#line 25
layout(location = 1)
in vec4 input_outerRect_0;


#line 25
layout(location = 2)
in vec4 input_shadowRect_0;


#line 25
layout(location = 3)
in vec4 input_borderRadius_0;


#line 25
layout(location = 4)
in float input_sigma_0;


#line 25
layout(location = 5)
in uint input_color_0;


#line 25
layout(location = 6)
in uint input_clipIndex_0;

struct Varyings_0
{
    vec4 position_0;
    vec2 pixelPos_0;
    vec4 shadowRect_0;
    vec4 borderRadius_0;
    float sigma_0;
    uint color_0;
    uint clipIndex_0;
};


#line 50
void main()
{

    vec2 pixelPos_1 = input_outerRect_0.xy + input_unitPos_0 * input_outerRect_0.zw;

#line 52
    Varyings_0 o_0;

    o_0.position_0 = (((vec4(pixelPos_1, 0.0, 1.0)) * (Globals_0.u_projection_0)));
    o_0.pixelPos_0 = pixelPos_1;
    o_0.shadowRect_0 = input_shadowRect_0;
    o_0.borderRadius_0 = input_borderRadius_0;
    o_0.sigma_0 = input_sigma_0;
    o_0.color_0 = input_color_0;
    o_0.clipIndex_0 = input_clipIndex_0;
    Varyings_0 _S1 = o_0;

#line 61
    gl_Position = o_0.position_0;

#line 61
    entryPointParam_vertexMain_pixelPos_0 = _S1.pixelPos_0;

#line 61
    entryPointParam_vertexMain_shadowRect_0 = _S1.shadowRect_0;

#line 61
    entryPointParam_vertexMain_borderRadius_0 = _S1.borderRadius_0;

#line 61
    entryPointParam_vertexMain_sigma_0 = _S1.sigma_0;

#line 61
    entryPointParam_vertexMain_color_0 = _S1.color_0;

#line 61
    entryPointParam_vertexMain_clipIndex_0 = _S1.clipIndex_0;

#line 61
    return;
}


#line 74
vec2 erf2_0(vec2 x_0)
{
    vec2 _S2 = vec2((ivec2(sign((x_0)))));
    vec2 a_0 = abs(x_0);
    vec2 v_0 = 1.0 + (0.27839300036430359 + (0.23038899898529053 + 0.07810799777507782 * (a_0 * a_0)) * a_0) * a_0;
    vec2 v_1 = v_0 * v_0;
    return _S2 - _S2 / (v_1 * v_1);
}


#line 91
float roundedBoxShadowX_0(float x_1, float y_0, float sigma_1, float corner_0, vec2 halfSize_0)
{
    float _S3 = min(halfSize_0.y - corner_0 - abs(y_0), 0.0);
    float curved_0 = halfSize_0.x - corner_0 + sqrt(max(0.0, corner_0 * corner_0 - _S3 * _S3));
    vec2 integral_0 = 0.5 + 0.5 * erf2_0(vec2(x_1 - curved_0, x_1 + curved_0) * (sqrt(0.5) / sigma_1));
    return integral_0.y - integral_0.x;
}


#line 83
float gaussian1D_0(float x_2, float sigma_2)
{

    return exp(- (x_2 * x_2) / (2.0 * sigma_2 * sigma_2)) / (sqrt(6.28318548202514648) * sigma_2);
}


#line 100
float roundedBoxShadow_0(vec2 lower_0, vec2 upper_0, vec2 point_0, float sigma_3, float corner_1)
{

    vec2 halfSize_1 = (upper_0 - lower_0) * 0.5;
    vec2 p_0 = point_0 - (lower_0 + upper_0) * 0.5;

    float _S4 = p_0.y;

#line 106
    float _S5 = halfSize_1.y;

#line 106
    float low_0 = _S4 - _S5;
    float high_0 = _S4 + _S5;
    float start_0 = clamp(-3.0 * sigma_3, low_0, high_0);


    float stepSize_0 = (clamp(3.0 * sigma_3, low_0, high_0) - start_0) / 4.0;
    float _S6 = start_0 + stepSize_0 * 0.5;

#line 112
    int i_0 = 0;

#line 112
    float y_1 = _S6;

#line 112
    float value_0 = 0.0;

    for(;;)
    {

#line 114
        if(i_0 < 4)
        {
        }
        else
        {

#line 114
            break;
        }
        float value_1 = value_0 + roundedBoxShadowX_0(p_0.x, _S4 - y_1, sigma_3, corner_1, halfSize_1) * gaussian1D_0(y_1, sigma_3) * stepSize_0;
        float y_2 = y_1 + stepSize_0;

#line 114
        i_0 = i_0 + 1;

#line 114
        y_1 = y_2;

#line 114
        value_0 = value_1;

#line 114
    }

#line 119
    return value_0;
}


#line 64
vec4 unpackARGB_0(uint c_0)
{

#line 70
    return vec4(float((c_0 >> 16) & 255U) / 255.0, float((c_0 >> 8) & 255U) / 255.0, float(c_0 & 255U) / 255.0, float((c_0 >> 24) & 255U) / 255.0);
}


#line 70
layout(location = 0)
out vec4 entryPointParam_fragmentMain_0;


#line 70
layout(location = 0)
in vec2 v_pixelPos_0;


#line 70
layout(location = 1)
in vec4 v_shadowRect_0;


#line 70
layout(location = 2)
in vec4 v_borderRadius_0;


#line 70
flat layout(location = 3)
in float v_sigma_0;


#line 70
flat layout(location = 4)
in uint v_color_0;


#line 70
flat layout(location = 5)
in uint v_clipIndex_0;


#line 123
void main()
{
    vec4 clip_0 = ClipRects_0.u_clipRects_0[v_clipIndex_0];

#line 125
    bool _S7;
    if((v_pixelPos_0.x) < (ClipRects_0.u_clipRects_0[v_clipIndex_0].x))
    {

#line 126
        _S7 = true;

#line 126
    }
    else
    {

#line 126
        _S7 = (v_pixelPos_0.x) >= (clip_0.z);

#line 126
    }

#line 126
    if(_S7)
    {

#line 126
        _S7 = true;

#line 126
    }
    else
    {

#line 126
        _S7 = (v_pixelPos_0.y) < (clip_0.y);

#line 126
    }
    if(_S7)
    {

#line 127
        _S7 = true;

#line 127
    }
    else
    {

#line 127
        _S7 = (v_pixelPos_0.y) >= (clip_0.w);

#line 127
    }

#line 126
    if(_S7)
    {

        discard;

#line 126
    }

#line 132
    vec2 lower_1 = v_shadowRect_0.xy;

#line 144
    vec4 rgba_0 = unpackARGB_0(v_color_0);

#line 144
    entryPointParam_fragmentMain_0 = vec4(rgba_0.xyz, rgba_0.w * clamp(roundedBoxShadow_0(lower_1, lower_1 + v_shadowRect_0.zw, v_pixelPos_0, v_sigma_0, min(max(max(v_borderRadius_0.x, v_borderRadius_0.y), max(v_borderRadius_0.z, v_borderRadius_0.w)), min(v_shadowRect_0.z, v_shadowRect_0.w) * 0.5)), 0.0, 1.0));

#line 144
    return;
}

