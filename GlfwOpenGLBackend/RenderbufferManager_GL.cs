using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using Framework.GLFW.NET;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend;

public class RenderbufferManager_GL : GpuResourceManager<IHandle<IGpuRenderbuffer>, GpuRenderbuffer_GL>, IRenderbufferManager
{
    public IGpuFramebufferHandle WindowBufferHandle { get; private set; }

    private readonly IGpuFramebuffer m_WindowFramebuffer;
    private readonly TextureManager_GL m_TextureManager;
    
    public RenderbufferManager_GL(IWindow window, TextureManager_GL textureManager)
    {
        m_WindowFramebuffer = window.Framebuffer;
        m_TextureManager = textureManager;
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
    
    public void BindWindow()
    {
        Bind(null);
    }

    public void ClearColorBuffers(float r, float g, float b, float a)
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
    
    private readonly Dictionary<(int, bool), Stack<IGpuRenderbufferHandle>> m_RenderBufferPool = new();

    public IGpuRenderbufferHandle GetTempRenderbuffer(int colorBuffersCount, bool createDepthBuffer)
    {
        var key = (colorBuffersCount, createDepthBuffer);
        if (!m_RenderBufferPool.TryGetValue(key, out var pool))
        {
            pool = new Stack<IGpuRenderbufferHandle>();
            m_RenderBufferPool[key] = pool;
        }

        if (pool.Count > 0)
        {
            var renderBuffer = pool.Pop();
            return renderBuffer;
        }
        else
        {
            var width = WindowBufferHandle.Width;
            var height = WindowBufferHandle.Height;
            var renderBuffer =
                new GpuRenderbuffer_GL(m_TextureManager, width, height, colorBuffersCount, createDepthBuffer);
            var handle = new GpuRenderbufferHandle(renderBuffer);
            Add(handle, renderBuffer);
            pool.Push(handle);
            return handle;
        }

       
    }

    public void ReleaseTempRenderbuffer(IGpuRenderbufferHandle tempRenderbufferHandle)
    {
        var key = (tempRenderbufferHandle.ColorBuffers.Length, tempRenderbufferHandle.HasDepthBuffer);
        m_RenderBufferPool[key].Push(tempRenderbufferHandle);
    }
}