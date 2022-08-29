using EasyGameFramework.API;

namespace Framework;

public class ScriptableRenderer : IRenderer
{
    private UnlitRenderPass m_UnlitRenderPass;
    private SpecularRenderPass m_SpecularRenderPass;

    public ScriptableRenderer()
    {
        m_UnlitRenderPass = new UnlitRenderPass();
        m_SpecularRenderPass = new SpecularRenderPass();
    }
    
    public void Add(IRenderable renderable)
    {
    }

    public void Remove(IRenderable renderable)
    {
    }

    public void Render(IGpu gpu, ICamera camera)
    {
    }
}