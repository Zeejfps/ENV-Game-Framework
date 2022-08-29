using System.Drawing;
using System.Numerics;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;

namespace Framework;

public class TestLight : ISceneObject
{
    public ITransform3D Transform { get; }
    public float Intensity { get; set; }
    public Color Color;

    private IHandle<IGpuMesh> m_Mesh;

    private readonly UnlitRenderPass m_Renderer;

    public TestLight(UnlitRenderPass renderer, ITransform3D transform)
    {
        Transform = transform;
        m_Renderer = renderer;
    }
    
    public void Load(IScene scene)
    {
        var gpu = scene.App.Gpu;
        m_Mesh = gpu.LoadMesh("Assets/Meshes/quad.mesh");
        m_Renderer.Add(new UnlitRendererable
        {
            MeshHandle = m_Mesh,
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