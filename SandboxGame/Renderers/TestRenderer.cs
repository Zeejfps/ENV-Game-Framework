using EasyGameFramework.API;
using Framework.Materials;

namespace Framework;

public class TestRenderer : IRenderer
{
    private readonly IGpu m_Gpu;
    private readonly UnlitRenderPass m_UnlitRenderPass;
    private readonly SpecularRenderPass m_SpecularRenderPass;
    private readonly FullScreenBlitPass m_FullScreenBlitPass;

    public TestRenderer(IGpu gpu)
    {
        m_Gpu = gpu;
        m_UnlitRenderPass = new UnlitRenderPass(UnlitMaterial.Load(gpu));
        m_SpecularRenderPass = new SpecularRenderPass();
        m_FullScreenBlitPass = new FullScreenBlitPass();
    }

    public void Render(ICamera camera)
    {
        var gpu = m_Gpu;
        var renderbufferManager = gpu.RenderbufferManager;
        var windowFramebufferWidth = renderbufferManager.WindowBufferHandle.Width;
        var windowFramebufferHeight = renderbufferManager.WindowBufferHandle.Height;

        var tempRenderbufferHandle =
            renderbufferManager.GetTempRenderbuffer(3, true);
        
        renderbufferManager.Bind(tempRenderbufferHandle);
        renderbufferManager.SetSize(windowFramebufferWidth, windowFramebufferHeight);
        renderbufferManager.ClearColorBuffer(0f, 0f, 0f, 0f);
        
        //m_SpecularRenderPass.Render(gpu, camera, renderScene);
        
        renderbufferManager.BindWindow();
        renderbufferManager.ClearColorBuffer(.42f, .607f, .82f, 1f);
        // m_FullScreenBlitPass.Render(gpu, m_QuadMeshHandle,
        //     m_FullScreenBlitShaderHandle,
        //     m_TempRenderbufferHandle.ColorBuffers[0],
        //     m_TempRenderbufferHandle.ColorBuffers[1],
        //     m_TempRenderbufferHandle.ColorBuffers[2]);
        //
        m_UnlitRenderPass.Render(gpu, camera);

        renderbufferManager.ReleaseTempRenderbuffer(tempRenderbufferHandle);
    }
}