using System.Diagnostics;
using System.Numerics;
using Framework.Common;
using TicTacToePrototype;

namespace Framework;

public class Ship : ISceneObject
{
    public ITransform3D Transform { get; }
    
    private IGpuMesh? m_Mesh;
    private IGpuTexture? m_Diffuse;
    private IGpuTexture? m_Normal;
    private IGpuTexture? m_Roughness;
    private IGpuTexture? m_Occlusion;
    private IGpuTexture? m_Translucency;

    private readonly SpecularRenderPass m_SpecularRenderPass;

    public Ship(SpecularRenderPass specularRenderPass)
    {
        Transform = new Transform3D();
        m_SpecularRenderPass = specularRenderPass;
    }
    
    public void Load(IScene scene)
    {
        var assetDatabase = scene.Context.AssetDatabase;
        m_Mesh = assetDatabase.Load<IGpuMesh>("Assets/Meshes/ship.mesh");
        m_Diffuse = assetDatabase.Load<IGpuTexture>("Assets/Textures/Ship/ship_d.texture");
        m_Normal = assetDatabase.Load<IGpuTexture>("Assets/Textures/Ship/ship_n.texture");
        m_Roughness = assetDatabase.Load<IGpuTexture>("Assets/Textures/Ship/ship_r.texture");
        m_Occlusion = assetDatabase.Load<IGpuTexture>("Assets/Textures/Ship/ship_ao.texture");
        m_Translucency = assetDatabase.Load<IGpuTexture>("Assets/Textures/Toad/Toad_Translucency.texture");
        
        m_SpecularRenderPass.Register(new SpecularRenderable
        {
            Mesh = m_Mesh,
            Transform = Transform,
            Textures = new SpecularRenderableTextures
            {
                Diffuse = m_Diffuse,
                Normal = m_Normal,
                Occlusion = m_Occlusion,
                Roughness = m_Roughness,
                Translucency = m_Translucency
            }
        });
    }
    
    public void Update(IScene scene)
    {
        
    }

    public void Unload(IScene scene)
    {
        
    }
}