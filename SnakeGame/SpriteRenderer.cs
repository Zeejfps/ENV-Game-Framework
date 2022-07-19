 using System.Numerics;
 using Framework;

 namespace SnakeGame;

public class SpriteRenderer
{
    private readonly IMaterial m_Material;
    private readonly IMesh m_QuadMesh;

    public SpriteRenderer(IMesh quadMesh, IMaterial material)
    {
        m_QuadMesh = quadMesh;
        m_Material = material;
    }

    public void Render(ICamera camera)
    {
        using var materialHandle = m_Material.Use();
        using var meshHandle = m_QuadMesh.Use();

        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
            
        materialHandle.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        materialHandle.SetMatrix4x4("matrix_view", viewMatrix);
        materialHandle.SetMatrix4x4("matrix_model", Matrix4x4.Identity);
        materialHandle.SetVector3("color", new Vector3(1f, 0f, 1f));

        meshHandle.Render();
    }
}