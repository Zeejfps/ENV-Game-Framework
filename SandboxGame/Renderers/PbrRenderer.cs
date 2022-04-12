namespace Framework;

public class PbrRenderer : ISceneObject
{
    private IMaterial m_Material;
    private IFramebuffer m_Framebuffer;

    private List<PbrRenderable> m_Renderables = new List<PbrRenderable>();
    
    public void Load(IScene scene)
    {
        m_Framebuffer = scene.Context.CreateFramebuffer(scene.Context.Window.Width, scene.Context.Window.Height);
    }

    public void Update(IScene scene)
    {
        
    }

    public void Unload(IScene scene)
    {
        
    }

    public void Add(PbrRenderable renderable)
    {
        m_Renderables.Add(renderable);
    }
}

public class PbrRenderable
{
    
}