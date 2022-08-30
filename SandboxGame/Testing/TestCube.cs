using EasyGameFramework;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using Framework;
using TicTacToePrototype;

namespace Framework;

public class TestCube : ISceneObject
{
    public ITransform3D Transform => m_Transform;
    
    private IHandle<IGpuMesh> m_Mesh;
    
    private readonly ITransform3D m_Transform;
    private readonly SpecularRenderPass m_BlinnRenderPass;

    public TestCube(SpecularRenderPass blinnRenderPass)
    {
        m_BlinnRenderPass = blinnRenderPass;
        m_Transform = new Transform3D();
    }
    
    public void Load(IScene scene)
    {
        var gpu = scene.Context.Gpu;
        m_Mesh = gpu.Mesh.Load("Assets/Meshes/ship.mesh");
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