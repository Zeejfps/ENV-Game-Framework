using System.Diagnostics;
using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;

namespace SampleGames;

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
        Snake snake,
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

        //var bufferHandle = shader.GetBuffer("model_matrices_t");
        
        foreach (var segment in snake.Segments)
        {
            if (segment == snake.Head)
                shader.SetVector3("color", new Vector3(0.1f, 1f, 0.1f));
            else
                shader.SetVector3("color", new Vector3(1f, 0f, 1f));
        
            var worldMatrix = Matrix4x4.CreateScale(0.5f)
                              * Matrix4x4.CreateTranslation(segment.X + 0.5f, segment.Y + 0.5f, 0f);
                
            shader.SetMatrix4x4("matrix_model", worldMatrix);
            mesh.Render();
        }
    }
    
}