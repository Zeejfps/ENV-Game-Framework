using ENV.Engine;
using TicTacToePrototype;

namespace ENV;

public class TestCube : ISceneObject
{
    public ITransform Transform => m_Transform;
    
    private IMesh m_Mesh;
    
    private readonly ITransform m_Transform;
    private readonly BlinnRenderer m_BlinnRenderer;

    public TestCube(BlinnRenderer blinnRenderer)
    {
        m_BlinnRenderer = blinnRenderer;
        m_Transform = new Transform3D();
    }
    
    public void Load(IScene scene)
    {
        var assetLoader = scene.Context.AssetLoader;
        m_Mesh = assetLoader.LoadAsset<IMesh>("Assets/Meshes/Monkey.obj");
    }

    public void Update(IScene scene)
    {
        m_BlinnRenderer.Render(new BlinnRenderData
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