using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

internal class RenderbufferManager_GL : GpuResourceManager<IHandle<IGpuRenderbuffer>, GpuRenderbuffer_GL>,
    IRenderbufferManager
{
    private readonly Dictionary<(int, bool), Stack<IGpuRenderbufferHandle>> m_RenderBufferPool = new();
    private readonly TextureManager_GL m_TextureManager;

    private readonly IGpuFramebuffer m_WindowFramebuffer;

    public RenderbufferManager_GL(TextureManager_GL textureManager, IGpuFramebuffer windowFramebuffer)
    {
        m_WindowFramebuffer = windowFramebuffer;
        m_TextureManager = textureManager;
        WindowBufferHandle = new GpuWindowFramebufferHandle(m_WindowFramebuffer);
    }

    public IGpuFramebufferHandle WindowBufferHandle { get; }

    public float Width
    {
        get
        {
            if (BoundResource == null)
                return WindowBufferHandle.Width;
            return BoundResource.Width;
        }
    }

    public float Height
    {
        get
        {
            if (BoundResource == null)
                return WindowBufferHandle.Height;
            return BoundResource.Height;
        }
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

    protected override GpuRenderbuffer_GL LoadAndBindResource(string assetPath)
    {
        throw new NotImplementedException();
    }

    protected override IHandle<IGpuRenderbuffer> CreateHandle(GpuRenderbuffer_GL resource)
    {
        return new GpuRenderbufferHandle(resource);
    }

    public void BindToWindow()
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

    public IGpuRenderbufferHandle CreateRenderbuffer(int colorBuffersCount, bool createDepthBuffer, int width, int height)
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
            var renderBuffer = new GpuRenderbuffer_GL(m_TextureManager, width, height, colorBuffersCount, createDepthBuffer);
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

    public void Blit(IHandle<IGpuRenderbuffer> src)
    {
        var dstFramebuffer = m_WindowFramebuffer;
        if (BoundResource != null)
        {
            dstFramebuffer = BoundResource;
        }

        Blit(src, 0, 0, dstFramebuffer.Width, dstFramebuffer.Height);
    }

    public void Blit(IHandle<IGpuRenderbuffer> src, int dstX, int dstY, int dstWidth, int dstHeight)
    {
        var srcFramebuffer = Get(src);
        uint dstFramebufferId = 0;
        if (BoundResource != null)
            dstFramebufferId = BoundResource.Id;
        
        glBindFramebuffer(GL_READ_FRAMEBUFFER, srcFramebuffer.Id);
        glBindFramebuffer(GL_DRAW_FRAMEBUFFER, dstFramebufferId);
        glBlitFramebuffer(
            0, 0, srcFramebuffer.Width, srcFramebuffer.Height, 
            dstX, dstY, dstWidth, dstHeight, 
            GL_COLOR_BUFFER_BIT, GL_NEAREST
        );
    }
}