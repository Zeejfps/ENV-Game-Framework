#pragma ps pixelShader
#pragma vs vertexShader

struct vertexInfo
{
    float3 position : POSITION;
    //float2 uv: TEXCOORD0;
    float4 color : COLOR;
};

struct v2p
{
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 color : TEXCOORD1;
};

//texture MainTexture;
// uniforms
//sampler2D MyTexture = sampler_state
//{
//    Texture = (ModelTexture));
//};

//float2 UVTile;
float4x4 worldViewProjection;

//vert shader func
v2p vertexShader(vertexInfo input)
{
    v2p output;
    output.position = mul(worldViewProjection, float4(input.position,1.0));
    //output.uv = input.uv * UVTile;
    //output.color = input.color;
    return output;
}

// pixel shader func
float4 pixelShader(v2p input) : SV_TARGET
{
    //float4 color = tex2D(MyTexture, input.uv);
    return float4(1,0,0,1);
}