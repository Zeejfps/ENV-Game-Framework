// Generated from ZGF.Gui.Shaders/canvas_image.slang by ZGF.Gui.Shaders/regenerate.cs. Edit the .slang and regenerate.
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

#line 4
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

#line 3 1
layout(binding = 2)
uniform sampler2D u_texture_0;


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
in vec2 v_uv_0;


#line 45
layout(location = 2)
in vec2 v_local_0;


#line 45
flat layout(location = 3)
in vec2 v_halfSize_0;


#line 45
flat layout(location = 4)
in vec4 v_srcUV_0;


#line 45
flat layout(location = 5)
in float v_fadesEdges_0;


#line 45
flat layout(location = 6)
in uint v_tint_0;


#line 45
flat layout(location = 7)
in uint v_clipIndex_0;


#line 45
flat layout(location = 8)
in vec4 v_cornerRadius_0;


#line 72 1
void main()
{
    float clipCover_0 = clipCoverage_0(v_pixelPos_0, v_clipIndex_0);
    if(outsideClipRect_0(v_pixelPos_0, v_clipIndex_0))
    {

#line 75
        discard;

#line 75
    }
    if(clipCover_0 <= 0.0)
    {

#line 76
        discard;

#line 76
    }


    vec2 devicePerLogical_0 = Globals_0.canvasMetrics_0.zw / Globals_0.canvasMetrics_0.xy;

#line 79
    float coverage_0;



    if(v_fadesEdges_0 > 0.0)
    {
        vec2 inside_0 = (v_halfSize_0 - abs(v_local_0)) * devicePerLogical_0;
        float coverage_1 = clamp(inside_0.x + 0.5, 0.0, 1.0) * clamp(inside_0.y + 0.5, 0.0, 1.0);
        if(coverage_1 <= 0.0)
        {

#line 87
            discard;

#line 87
        }

#line 87
        coverage_0 = coverage_1;

#line 83
    }
    else
    {

#line 83
        coverage_0 = 1.0;

#line 83
    }

#line 83
    float radius_0;

#line 93
    if((v_local_0.y) > 0.0)
    {

#line 93
        if((v_local_0.x) > 0.0)
        {

#line 93
            radius_0 = v_cornerRadius_0.y;

#line 93
        }
        else
        {

#line 93
            radius_0 = v_cornerRadius_0.x;

#line 93
        }

#line 93
    }
    else
    {

#line 94
        if((v_local_0.x) > 0.0)
        {

#line 94
            radius_0 = v_cornerRadius_0.z;

#line 94
        }
        else
        {

#line 94
            radius_0 = v_cornerRadius_0.w;

#line 94
        }

#line 93
    }

    if(radius_0 > 0.0)
    {
        vec2 q_1 = abs(v_local_0) - (v_halfSize_0 - vec2(radius_0));

        float coverage_2 = coverage_0 * clamp(0.5 - (length(max(q_1, vec2(0.0))) + min(max(q_1.x, q_1.y), 0.0) - radius_0) * devicePerLogical_0.x, 0.0, 1.0);
        if(coverage_2 <= 0.0)
        {

#line 100
            discard;

#line 100
        }

#line 100
        coverage_0 = coverage_2;

#line 95
    }
    else
    {

#line 95
    }

#line 104
    vec2 uvEnd_0 = v_srcUV_0.xy + v_srcUV_0.zw;

#line 104
    entryPointParam_fragmentMain_0 = (texture((u_texture_0), (clamp(v_uv_0, min(v_srcUV_0.xy, uvEnd_0), max(v_srcUV_0.xy, uvEnd_0))))) * unpackARGB_0(v_tint_0) * vec4(1.0, 1.0, 1.0, coverage_0 * clipCover_0);

#line 104
    return;
}

