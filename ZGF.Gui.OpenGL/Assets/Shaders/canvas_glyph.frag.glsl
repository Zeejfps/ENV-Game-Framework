// Generated from ZGF.Gui.Shaders/canvas_glyph.slang by ZGF.Gui.Shaders/regenerate.cs. Edit the .slang and regenerate.
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

#line 3 1
layout(binding = 2)
uniform sampler2D u_atlas_0;


#line 50 0
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
layout(location = 0)
in vec2 v_pixelPos_0;


#line 45
layout(location = 1)
in vec2 v_atlasUV_0;


#line 45
flat layout(location = 2)
in uint v_color_0;


#line 45
flat layout(location = 3)
in uint v_clipIndex_0;


#line 48 1
void main()
{
    float clipCover_0 = clipCoverage_0(v_pixelPos_0, v_clipIndex_0);
    if(outsideClipRect_0(v_pixelPos_0, v_clipIndex_0))
    {

#line 51
        discard;

#line 51
    }
    if(clipCover_0 <= 0.0)
    {

#line 52
        discard;

#line 52
    }

    float alpha_0 = (texture((u_atlas_0), (v_atlasUV_0))).x;
    if(alpha_0 <= 0.0)
    {

#line 55
        discard;

#line 55
    }

    vec4 color_0 = unpackARGB_0(v_color_0);

#line 57
    entryPointParam_fragmentMain_0 = vec4(color_0.xyz, color_0.w * alpha_0 * clipCover_0);

#line 57
    return;
}

