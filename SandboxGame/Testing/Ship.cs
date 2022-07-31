using System.Diagnostics;
using System.Numerics;
using Framework.Common;
using TicTacToePrototype;

namespace Framework;

public class Ship : ISceneObject
{
    public ITransform3D Transform { get; }
    
    private IMesh? m_Mesh;
    private ITexture? m_Diffuse;
    private ITexture? m_Normal;
    private ITexture? m_Roughness;
    private ITexture? m_Occlusion;
    private ITexture? m_Translucency;

    private readonly SpecularRenderPass m_SpecularRenderPass;

    public Ship(SpecularRenderPass specularRenderPass)
    {
        Transform = new Transform3D();
        m_SpecularRenderPass = specularRenderPass;
    }
    
    public void Load(IScene scene)
    {
        var assetDatabase = scene.Context.AssetDatabase;
        m_Mesh = assetDatabase.Load<IMesh>("Assets/Meshes/ship.mesh");
        m_Diffuse = assetDatabase.Load<ITexture>("Assets/Textures/Ship/ship_d.texture");
        m_Normal = assetDatabase.Load<ITexture>("Assets/Textures/Ship/ship_n.texture");
        m_Roughness = assetDatabase.Load<ITexture>("Assets/Textures/Ship/ship_r.texture");
        m_Occlusion = assetDatabase.Load<ITexture>("Assets/Textures/Ship/ship_ao.texture");
        m_Translucency = assetDatabase.Load<ITexture>("Assets/Textures/Toad/Toad_Translucency.texture");
        
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