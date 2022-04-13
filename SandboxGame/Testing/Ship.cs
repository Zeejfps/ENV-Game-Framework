using System.Diagnostics;
using TicTacToePrototype;

namespace Framework;

public class Ship : ISceneObject
{
    public ITransform Transform { get; }
    
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
        m_Mesh = assetDatabase.LoadAsset<IMesh>("Assets/Meshes/ship.mesh");
        m_Diffuse = assetDatabase.LoadAsset<ITexture>("Assets/Textures/Ship/ship_d.texture");
        m_Normal = assetDatabase.LoadAsset<ITexture>("Assets/Textures/Ship/ship_n.texture");
        m_Roughness = assetDatabase.LoadAsset<ITexture>("Assets/Textures/Ship/ship_r.texture");
        m_Occlusion = assetDatabase.LoadAsset<ITexture>("Assets/Textures/Ship/ship_ao.texture");
        m_Translucency = assetDatabase.LoadAsset<ITexture>("Assets/Textures/Toad/Toad_Translucency.texture");
    }

    public void Update(IScene scene)
    {
        Debug.Assert(m_Mesh != null);
        Debug.Assert(m_Diffuse != null);
        Debug.Assert(m_Normal != null);
        Debug.Assert(m_Occlusion != null);
        Debug.Assert(m_Roughness != null);
        Debug.Assert(m_Translucency != null);
        
        m_SpecularRenderPass.Submit(new SpecularRenderable
        {
            Mesh = m_Mesh,
            WorldMatrix = Transform.WorldMatrix,
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

    public void Unload(IScene scene)
    {
        
    }
}