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
    private readonly UnlitMaterial m_Material;

    public TestLight(UnlitMaterial material, ITransform3D transform)
    {
        m_Material = material;
        Transform = transform;
    }
    
    public void Load(IScene scene)
    {
        var gpu = scene.App.Gpu;
        m_Mesh = gpu.LoadMesh("Assets/Meshes/quad.mesh");
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