using Framework;
using TicTacToePrototype;

namespace Framework;

public class TestCube : ISceneObject
{
    public ITransform Transform => m_Transform;
    
    private IMesh m_Mesh;
    
    private readonly ITransform m_Transform;
    private readonly SpecularRenderer m_BlinnRenderer;

    public TestCube(SpecularRenderer blinnRenderer)
    {
        m_BlinnRenderer = blinnRenderer;
        m_Transform = new Transform3D();
    }
    
    public void Load(IScene scene)
    {
        var assetLoader = scene.Context.AssetDatabase;
        m_Mesh = assetLoader.LoadAsset<IMesh>("Assets/Meshes/Toad.mesh");
    }

    public void Update(IScene scene)
    {
        m_BlinnRenderer.Render(new SpecularRendererData
        {
            Mesh = m_Mesh,
            Transform = m_Transform,
        });
    }

    public void Unload(IScene scene)
    {
        m_Mesh.Unload();
    }
}