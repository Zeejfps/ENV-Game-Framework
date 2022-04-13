using System.Numerics;
using Framework;

namespace Framework;

public class TestLight : ISceneObject
{
    public ITransform Transform { get; }
    
    private IMesh m_Mesh;

    private readonly UnlitRenderPass m_Renderer;

    public TestLight(UnlitRenderPass renderer, ITransform transform)
    {
        Transform = transform;
        m_Renderer = renderer;
    }
    
    public void Load(IScene scene)
    {
        m_Mesh = scene.Context.AssetDatabase.LoadAsset<IMesh>("Assets/Meshes/Light.mesh");
        m_Renderer.Add(new UnlitRendererable
        {
            Mesh = m_Mesh,
            Transform = Transform,
            Color = new Vector3(1f, 0f, 0.5f)
        });
    }

    public void Update(IScene scene)
    {
        
    }

    public void Unload(IScene scene)
    {
    }
}