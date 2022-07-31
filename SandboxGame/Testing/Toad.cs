using System.Diagnostics;
using Framework.Common;
using TicTacToePrototype;

namespace Framework;

public class Toad : ISceneObject
{
    public ITransform3D Transform { get; }
    
    private IMesh? m_Mesh;
    private ITexture? m_Diffuse;
    private ITexture? m_Normal;
    private ITexture? m_Roughness;
    private ITexture? m_Occlusion;
    private ITexture? m_Translucency;

    private readonly SpecularRenderPass m_SpecularRenderPass;

    public Toad(SpecularRenderPass specularRenderPass)
    {
        Transform = new Transform3D();
        m_SpecularRenderPass = specularRenderPass;
    }
    
    public void Load(IScene scene)
    {
        var assetDatabase = scene.Context.AssetDatabase;
        m_Mesh = assetDatabase.Load<IMesh>("Assets/Meshes/Toad.mesh");
        m_Diffuse = assetDatabase.Load<ITexture>("Assets/Textures/Toad/Toad_BaseColor.texture");
        m_Normal = assetDatabase.Load<ITexture>("Assets/Textures/Toad/Toad_Normal.texture");
        m_Roughness = assetDatabase.Load<ITexture>("Assets/Textures/Toad/Toad_Roughness.texture");
        m_Occlusion = assetDatabase.Load<ITexture>("Assets/Textures/Toad/Toad_AO.texture");
        m_Translucency = assetDatabase.Load<ITexture>("Assets/Textures/Toad/Toad_Translucency.texture");

        m_SpecularRenderPass.Register(new SpecularRenderable
        {
            Transform = Transform,
            Mesh = m_Mesh,
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
        Debug.Assert(m_Mesh != null);
        Debug.Assert(m_Diffuse != null);
        Debug.Assert(m_Normal != null);
        Debug.Assert(m_Occlusion != null);
        Debug.Assert(m_Roughness != null);
        Debug.Assert(m_Translucency != null);
    }

    public void Unload(IScene scene)
    {
        
    }
}