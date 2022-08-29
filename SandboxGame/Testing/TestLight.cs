using System.Drawing;
using System.Numerics;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using Framework;

namespace Framework;

public class TestLight : ISceneObject
{
    public ITransform3D Transform { get; }
    public float Intensity { get; set; }
    public Color Color;

    private IGpuMesh m_Mesh;

    private readonly UnlitRenderPass m_Renderer;

    public TestLight(UnlitRenderPass renderer, ITransform3D transform)
    {
        Transform = transform;
        m_Renderer = renderer;
    }
    
    public void Load(IScene scene)
    {
        var locator = scene.Context.Locator;
        var meshLoader = locator.LocateOrThrow<IAssetLoader<IGpuMesh>>();
        m_Mesh = meshLoader.Load("Assets/Meshes/quad.mesh");
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