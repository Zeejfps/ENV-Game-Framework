 using Framework;

 namespace SnakeGame;

public class SnakeRenderPass
{
    private readonly IMaterial m_Material;
    private readonly IMesh m_QuadMesh;
    private readonly IContext m_Context;

    public SnakeRenderPass(IContext context)
    {
        m_Context = context;
        m_QuadMesh = m_Context.AssetDatabase.LoadAsset<IMesh>("Assets/quad.mesh");
    }

    public void Render(ICamera camera)
    {
        
    }
}