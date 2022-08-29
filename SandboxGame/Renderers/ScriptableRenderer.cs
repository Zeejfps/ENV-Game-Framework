using EasyGameFramework.API;

namespace Framework;

public class ScriptableRenderer : IRenderer
{
    private UnlitRenderPass m_UnlitRenderPass;
    private SpecularRenderPass m_SpecularRenderPass;
    private FullScreenBlitPass m_FullScreenBlitPass;

    public ScriptableRenderer()
    {
        m_UnlitRenderPass = new UnlitRenderPass();
        m_SpecularRenderPass = new SpecularRenderPass();
        m_FullScreenBlitPass = new FullScreenBlitPass();
    }

    public void Render(IGpu gpu, ICamera camera, IRenderScene renderScene)
    {
        var renderbufferManager = gpu.RenderbufferManager;
        var windowFramebufferWidth = renderbufferManager.WindowBufferHandle.Width;
        var windowFramebufferHeight = renderbufferManager.WindowBufferHandle.Height;

        // var tempRenderbufferHandle =
        //     renderbufferManager.GetTempRenderbuffer(windowFramebufferWidth, windowFramebufferHeight);
        //
        // renderbufferManager.Bind(tempRenderbufferHandle);
        // renderbufferManager.SetSize(windowFramebufferWidth, windowFramebufferHeight);
        // renderbufferManager.ClearColorBuffer(0f, 0f, 0f, 0f);
        //m_SpecularRenderPass.Render(gpu, camera, renderScene);
        
        // renderbufferManager.BindWindow();
        // renderbufferManager.ClearColorBuffer(.42f, .607f, .82f, 1f);
        // m_FullScreenBlitPass.Render(gpu, m_QuadMeshHandle,
        //     m_FullScreenBlitShaderHandle,
        //     m_TempRenderbufferHandle.ColorBuffers[0],
        //     m_TempRenderbufferHandle.ColorBuffers[1],
        //     m_TempRenderbufferHandle.ColorBuffers[2]);
        //
        // m_UnlitRenderPass.Render(gpu, camera, m_UnlitShaderHandle);

        //renderbufferManager.ReleaseTempRenderbuffer(tempRenderbufferHandle);
    }
}