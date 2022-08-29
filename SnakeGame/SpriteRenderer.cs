using System.Numerics;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;

namespace SnakeGame;

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
    
    public void Render(ICamera camera, IHandle<IGpuShader> gpuShaderHandle, IGpuMesh quadMesh, IEnumerable<ITransform3D> transforms)
    {
        using var shader = gpuShaderHandle.Use();
        using var meshHandle = quadMesh.Use();

        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
            
        shader.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        shader.SetMatrix4x4("matrix_view", viewMatrix);
        shader.SetVector3("color", new Vector3(1f, 0f, 1f));

        foreach (var transform in transforms)
        {
            shader.SetMatrix4x4("matrix_model", transform.WorldMatrix);
            meshHandle.Render();
        }
    }
}