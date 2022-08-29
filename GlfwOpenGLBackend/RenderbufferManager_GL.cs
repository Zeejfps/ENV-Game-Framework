using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using Framework.GLFW.NET;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend;

public class RenderbufferManager_GL : GpuResourceManager<IHandle<IGpuRenderbuffer>, GpuRenderbuffer_GL>, IRenderbufferManager
{
    public IGpuFramebufferHandle WindowBufferHandle { get; private set; }

    private readonly IGpuFramebuffer m_WindowFramebuffer;
    
    public RenderbufferManager_GL(IGpuFramebuffer windowFramebuffer)
    {
        m_WindowFramebuffer = windowFramebuffer;
        WindowBufferHandle = new GpuWindowFramebufferHandle(m_WindowFramebuffer);
    }
    
    protected override void OnBound(GpuRenderbuffer_GL resource)
    {
        glBindFramebuffer(resource.Id);
        glViewport(0, 0, resource.Width, resource.Height);
    }

    protected override void OnUnbound()
    {
        glBindFramebuffer(0);
    }
    
    public void UseWindow()
    {
        Use(null);
    }

    public void ClearColor(float r, float g, float b, float a)
    {
        if (BoundResource == null)
            m_WindowFramebuffer.Clear(r, g, b, a);
        else
            BoundResource.Clear(r, g, b, a);
    }

    public void SetSize(int width, int height)
    {
        if (BoundResource == null)
            m_WindowFramebuffer.SetSize(width, height);
        else
            BoundResource.SetSize(width, height);
    }
}