using System.Diagnostics;
using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;

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
        
        shader.SetVector3("color", new Vector3(0f, 1f, 0f));
        shader.SetMatrix4x4("matrix_model", transforms[0].WorldMatrix);
        mesh.Render();


        for (var i = 1; i < transforms.Count; i++)
        {
            shader.SetVector3("color", new Vector3(i*10 / 20f, i * 10 / 20f, 1f));
            var transform = transforms[i];
            shader.SetMatrix4x4("matrix_model", transform.WorldMatrix);
            mesh.Render();
        }
    }
    
}