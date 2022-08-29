using System.Drawing;
using System.Numerics;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using Framework.Materials;

namespace Framework;

public class TestLight : ISceneObject
{
    public ITransform3D Transform { get; }
    public float Intensity { get; set; }
    public Color Color;

    private IHandle<IGpuMesh> m_Mesh;

    private readonly UnlitRenderPass m_Renderer;

    private UnlitMaterial m_Material;

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

    public void Render(IRenderer renderer)
    {
        renderer.Render(m_Material, m_Mesh);
    }

    public void Update(IScene scene)
    {
        
    }

    public void Unload(IScene scene)
    {
    }
}