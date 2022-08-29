using System.Numerics;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;

namespace Framework.Materials;

public class UnlitMaterial : IMaterial
{
    public Matrix4x4 ProjectionMatrix { get; set; }
    public Matrix4x4 ViewMatrix { get; set; }
    
    // TODO: We need per object variables
    public Matrix4x4 ModelMatrix { get; set; }
    public Vector3 Color { get; set; }

    public void Apply(IGpuShader shader)
    {
        shader.SetMatrix4x4("matrix_projection", ProjectionMatrix);    
    }
}