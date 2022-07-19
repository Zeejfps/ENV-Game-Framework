using Framework;
using Framework.Common;
using TicTacToePrototype;

namespace Framework;

public class TestCube : ISceneObject
{
    public ITransform3D Transform => m_Transform;
    
    private IMesh m_Mesh;
    
    private readonly ITransform3D m_Transform;
    private readonly SpecularRenderPass m_BlinnRenderPass;

    public TestCube(SpecularRenderPass blinnRenderPass)
    {
        m_BlinnRenderPass = blinnRenderPass;
        m_Transform = new Transform3D();
    }
    
    public void Load(IScene scene)
    {
        var assetLoader = scene.Context.AssetDatabase;
        m_Mesh = assetLoader.LoadAsset<IMesh>("Assets/Meshes/ship.mesh");
    }

    public void Update(IScene scene)
    {
        // m_BlinnRenderer.Render(new SpecularRendererData
        // {
        //     Mesh = m_Mesh,
        //     Transform = m_Transform,
        // });
    }

    public void Unload(IScene scene)
    {
        m_Mesh.Unload();
    }
}