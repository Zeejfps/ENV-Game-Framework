using System.Diagnostics;
using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;

namespace Core;

public class SnakeRenderer
{
    private IGpu Gpu { get; }
    private IHandle<IGpuShader>? ShaderHandle { get; set; }
    private IHandle<IGpuMesh>? MeshHandle { get; set; }

    public SnakeRenderer(IGpu gpu)
    {
        Gpu = gpu;
    }

    public void LoadResources()
    {
        ShaderHandle = Gpu.Shader.Load("Assets/sprite");
        MeshHandle = Gpu.Mesh.Load("Assets/quad");
    }

    public void Render(
        IReadOnlyList<ITransform3D> transforms,
        ICamera camera)
    {
        Debug.Assert(ShaderHandle != null);
        Debug.Assert(MeshHandle != null);
        
        var shader = Gpu.Shader;
        var mesh = Gpu.Mesh;
        
        shader.Bind(ShaderHandle);
        mesh.Bind(MeshHandle);
        
        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
            
        shader.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        shader.SetMatrix4x4("matrix_view", viewMatrix);
        shader.SetVector3("color", new Vector3(1f, 0f, 1f));
        
        foreach (var transform in transforms)
        {
            shader.SetMatrix4x4("matrix_model", transform.WorldMatrix);
            mesh.Render();
        }
    }
    
}