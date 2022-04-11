using TicTacToePrototype;

namespace Framework;

public class Toad : ISceneObject
{
    public ITransform Transform { get; }
    
    private IMesh? m_Mesh;
    private ITexture? m_Diffuse;
    private ITexture? m_Normal;
    private ITexture? m_Roughness;
    private ITexture? m_Occlusion;
    private ITexture? m_Translucency;

    private readonly SpecularRenderer m_SpecularRenderer;

    public Toad(SpecularRenderer specularRenderer)
    {
        m_SpecularRenderer = specularRenderer;
        Transform = new Transform3D();
    }
    
    public void Load(IScene scene)
    {
        var assetDatabase = scene.Context.AssetDatabase;
        m_Mesh = assetDatabase.LoadAsset<IMesh>("Assets/Meshes/Toad.mesh");
        m_Diffuse = assetDatabase.LoadAsset<ITexture>("Assets/Textures/Toad/Toad_BaseColor.texture");
        m_Normal = assetDatabase.LoadAsset<ITexture>("Assets/Textures/Toad/Toad_Normal.texture");
        m_Roughness = assetDatabase.LoadAsset<ITexture>("Assets/Textures/Toad/Toad_Roughness.texture");
        m_Occlusion = assetDatabase.LoadAsset<ITexture>("Assets/Textures/Toad/Toad_AO.texture");
        m_Translucency = assetDatabase.LoadAsset<ITexture>("Assets/Textures/Toad/Toad_Translucency.texture");
    }

    public void Update(IScene scene)
    {
        m_SpecularRenderer.Render(new SpecularRendererData
        {
            Mesh = m_Mesh,
            Transform = Transform,
            Diffuse = m_Diffuse,
            Normal = m_Normal,
            Occlusion = m_Occlusion,
            Roughness = m_Roughness,
            Translucency = m_Translucency
        });
    }

    public void Unload(IScene scene)
    {
        
    }
}