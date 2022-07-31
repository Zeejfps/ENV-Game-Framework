using System.Diagnostics;
using Framework.Common;
using TicTacToePrototype;

namespace Framework;

public class Toad : ISceneObject
{
    public ITransform3D Transform { get; }
    
    private IGpuMesh? m_Mesh;
    private IGpuTexture? m_Diffuse;
    private IGpuTexture? m_Normal;
    private IGpuTexture? m_Roughness;
    private IGpuTexture? m_Occlusion;
    private IGpuTexture? m_Translucency;

    private readonly SpecularRenderPass m_SpecularRenderPass;

    public Toad(SpecularRenderPass specularRenderPass)
    {
        Transform = new Transform3D();
        m_SpecularRenderPass = specularRenderPass;
    }
    
    public void Load(IScene scene)
    {
        var assetDatabase = scene.Context.AssetService;
        m_Mesh = assetDatabase.Load<IGpuMesh>("Assets/Meshes/Toad.mesh");
        m_Diffuse = assetDatabase.Load<IGpuTexture>("Assets/Textures/Toad/Toad_BaseColor.texture");
        m_Normal = assetDatabase.Load<IGpuTexture>("Assets/Textures/Toad/Toad_Normal.texture");
        m_Roughness = assetDatabase.Load<IGpuTexture>("Assets/Textures/Toad/Toad_Roughness.texture");
        m_Occlusion = assetDatabase.Load<IGpuTexture>("Assets/Textures/Toad/Toad_AO.texture");
        m_Translucency = assetDatabase.Load<IGpuTexture>("Assets/Textures/Toad/Toad_Translucency.texture");

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