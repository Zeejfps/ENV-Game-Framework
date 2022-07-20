using System.Numerics;
using Framework;
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
    
    public void Render(ICamera camera, IMaterial material, IMesh quadMesh, IEnumerable<ITransform3D> transforms)
    {
        using var materialHandle = material.Use();
        using var meshHandle = quadMesh.Use();

        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
            
        materialHandle.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        materialHandle.SetMatrix4x4("matrix_view", viewMatrix);
        materialHandle.SetVector3("color", new Vector3(1f, 0f, 1f));

        foreach (var transform in transforms)
        {
            materialHandle.SetMatrix4x4("matrix_model", transform.WorldMatrix);
            meshHandle.Render();
        }
    }
}