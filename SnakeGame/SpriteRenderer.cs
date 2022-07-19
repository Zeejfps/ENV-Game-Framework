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
        
    }
}