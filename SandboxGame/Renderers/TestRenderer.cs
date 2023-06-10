using EasyGameFramework.Api;
using EasyGameFramework.Api.Rendering;
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
        m_SpecularRenderPass = new SpecularRenderPass(m_Gpu);
        m_FullScreenBlitPass = new FullScreenBlitPass();
    }

    public void Render(ICamera camera)
    {
        var gpu = m_Gpu;
        var renderbufferManager = gpu.Renderbuffer;
        var windowFramebufferWidth = renderbufferManager.WindowBufferHandle.Width;
        var windowFramebufferHeight = renderbufferManager.WindowBufferHandle.Height;

        var tempRenderbufferHandle =
            gpu.CreateRenderbuffer(3, true, windowFramebufferWidth, windowFramebufferHeight);
        
        renderbufferManager.Bind(tempRenderbufferHandle);
        renderbufferManager.SetSize(windowFramebufferWidth, windowFramebufferHeight);
        renderbufferManager.ClearColorBuffers(0f, 0f, 0f, 0f);
        
        //m_SpecularRenderPass.Render(gpu, camera, renderScene);
        
        renderbufferManager.BindToWindow();
        renderbufferManager.ClearColorBuffers(.42f, .607f, .82f, 1f);
        // m_FullScreenBlitPass.Render(gpu, m_QuadMeshHandle,
        //     m_FullScreenBlitShaderHandle,
        //     m_TempRenderbufferHandle.ColorBuffers[0],
        //     m_TempRenderbufferHandle.ColorBuffers[1],
        //     m_TempRenderbufferHandle.ColorBuffers[2]);
        //
        m_UnlitRenderPass.Render(gpu, camera);

        gpu.ReleaseRenderbuffer(tempRenderbufferHandle);
    }
}