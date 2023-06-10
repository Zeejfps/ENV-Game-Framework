using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;

namespace Framework;

public class TestCube : ISceneObject
{
    public ITransform3D Transform => m_Transform;
    
    private IHandle<IGpuMesh> m_Mesh;

    private readonly IGpu m_Gpu;
    private readonly ITransform3D m_Transform;
    private readonly SpecularRenderPass m_BlinnRenderPass;

    public TestCube(IGpu gpu, SpecularRenderPass blinnRenderPass)
    {
        m_Gpu = gpu;
        m_BlinnRenderPass = blinnRenderPass;
        m_Transform = new Transform3D();
    }
    
    public void Load(IScene scene)
    {
        var gpu = m_Gpu;
        m_Mesh = gpu.Mesh.Load("Assets/Meshes/ship");
    }

    public void Update(float dt)
    {
        // m_BlinnRenderer.Render(new SpecularRendererData
        // {
        //     Mesh = m_Mesh,
        //     Transform = m_Transform,
        // });
    }

    public void Unload(IScene scene)
    {
        //m_Mesh.Dispose();
    }

    public void Render()
    {
        
    }
}