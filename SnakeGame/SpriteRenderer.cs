using System.Numerics;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;

namespace Snake;

public struct Entity
{
    
}

public struct SnakeSegment
{
    
}

public class SpriteRenderer
{
    
    
    public SpriteRenderer()
    {
    }

    public void Add(Entity entity)
    {
        
    }

    public void Remove(Entity entity)
    {
        
    }
    
    public void Render(
        IGpu gpu,
        ICamera camera,
        IHandle<IGpuShader> gpuShaderHandle,
        IHandle<IGpuMesh> quadMeshHandle,
        IEnumerable<ITransform3D> transforms)
    {
        var shader = gpu.Shader;
        var mesh = gpu.Mesh;
        
        shader.Bind(gpuShaderHandle);
        mesh.Bind(quadMeshHandle);
        
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