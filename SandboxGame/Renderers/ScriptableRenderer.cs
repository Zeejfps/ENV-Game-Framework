using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;

namespace Framework;

public class ScriptableRenderer : IRenderer
{
    private readonly IGpu m_Gpu;
    private readonly UnlitRenderPass m_UnlitRenderPass;
    private readonly SpecularRenderPass m_SpecularRenderPass;
    private readonly FullScreenBlitPass m_FullScreenBlitPass;

    public ScriptableRenderer(IGpu gpu)
    {
        m_Gpu = gpu;
        m_UnlitRenderPass = new UnlitRenderPass();
        m_SpecularRenderPass = new SpecularRenderPass();
        m_FullScreenBlitPass = new FullScreenBlitPass();
    }

    public void Render(IGpu gpu, ICamera camera)
    {
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
        // m_UnlitRenderPass.Render(gpu, camera, m_UnlitShaderHandle);

        renderbufferManager.ReleaseTempRenderbuffer(tempRenderbufferHandle);
    }

    private readonly Dictionary<IMaterial, HashSet<IHandle<IGpuMesh>>> m_MaterialToMeshesMap = new();

    public void BeginFrame()
    {
        m_MaterialToMeshesMap.Clear();
    }

    public void Render(IMaterial material, IHandle<IGpuMesh> meshHandle)
    {
        if (!m_MaterialToMeshesMap.TryGetValue(material, out var meshes))
        {
            meshes = new HashSet<IHandle<IGpuMesh>>();
            m_MaterialToMeshesMap[material] = meshes;
        }
        
        meshes.Clear();
        meshes.Add(meshHandle);
    }

    public void EndFrame()
    {
        
    }
}