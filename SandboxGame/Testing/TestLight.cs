using System.Numerics;
using Framework;

namespace Framework;

public class TestLight : ISceneObject
{
    public ITransform Transform { get; }
    
    private IMesh m_Mesh;

    private readonly UnlitRenderer m_Renderer;

    public TestLight(UnlitRenderer renderer, ITransform transform)
    {
        Transform = transform;
        m_Renderer = renderer;
    }
    
    public void Load(IScene scene)
    {
        //m_Mesh = scene.Context.AssetDatabase.LoadAsset<IMesh>("Assets/Meshes/Light.obj");
    }

    public void Update(IScene scene)
    {
        // m_Renderer.Render(new UnlitRenderData
        // {
        //     Mesh = m_Mesh,
        //     Transform = Transform,
        //     Color = new Vector3(1f, 0f, 0.5f)
        // });
    }

    public void Unload(IScene scene)
    {
    }
}