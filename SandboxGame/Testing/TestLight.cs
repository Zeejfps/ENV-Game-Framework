using System.Drawing;
using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;
using Framework.Materials;

namespace Framework;

public class TestLight : ISceneObject
{
    public ITransform3D Transform { get; }
    public float Intensity { get; set; }
    public Color Color;

    private IGpu m_Gpu;
    private IHandle<IGpuMesh> m_Mesh;
    private readonly UnlitMaterial m_Material;

    public TestLight(IGpu gpu, UnlitMaterial material, ITransform3D transform)
    {
        m_Gpu = gpu;
        m_Material = material;
        Transform = transform;
    }
    
    public void Load(IScene scene)
    {
        var gpu = m_Gpu;
        m_Mesh = gpu.Mesh.Load("Assets/Meshes/quad");
    }

    public void Render()
    {
        m_Material.Batch(m_Mesh, new UnlitMaterial.Properties
        {
            Color = new Vector3(1f, 0f, 0.5f),
            ModelMatrix = Transform.WorldMatrix,
        });
    }

    private float t;
    
    public void Update(float dt)
    {
        t += dt;
        Transform.WorldPosition += new Vector3(MathF.Sin(t),0,0) * dt * 5;
    }

    public void Unload(IScene scene)
    {
    }
}