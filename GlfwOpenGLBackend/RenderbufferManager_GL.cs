using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using Framework.GLFW.NET;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend;

public class RenderbufferManager_GL : GpuResourceManager<IHandle<IGpuRenderbuffer>, TextureFramebuffer_GL>, IRenderbufferManager
{
    private readonly IGpuFramebuffer m_WindowFramebuffer;
    
    public RenderbufferManager_GL(IGpuFramebuffer windowFramebuffer)
    {
        m_WindowFramebuffer = windowFramebuffer;
    }
    
    protected override void OnBound(TextureFramebuffer_GL resource)
    {
        glBindFramebuffer(resource.Id);
        glViewport(0, 0, resource.Width, resource.Height);
    }

    protected override void OnUnbound()
    {
        glBindFramebuffer(0);
    }

    public int FramebufferWidth
    {
        get
        {
            if (BoundResource != null)
                return BoundResource.Width;
            else
                return m_WindowFramebuffer.Width;
        }
    }

    public int FramebufferHeight
    {
        get
        {
            if (BoundResource != null)
                return BoundResource.Height;
            else
                return m_WindowFramebuffer.Height;
        }
    }

    public IHandle<IGpuTexture>[] ColorBuffers
    {
        get
        {
            if (BoundResource != null)
                return BoundResource.ColorBuffers;
            return Array.Empty<IHandle<IGpuTexture>>();
        }
    }

    public IHandle<IGpuTexture>? DepthBuffer
    {
        get
        {
            if (BoundResource != null)
                return BoundResource.DepthBuffer;
            return null;
        }
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