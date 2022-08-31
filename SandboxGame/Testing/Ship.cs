using EasyGameFramework;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;

namespace Framework;

public class Ship : ISceneObject
{
    public ITransform3D Transform { get; }
    
    private IHandle<IGpuMesh>? m_Mesh;
    private IHandle<IGpuTexture>? m_Diffuse;
    private IHandle<IGpuTexture>? m_Normal;
    private IHandle<IGpuTexture>? m_Roughness;
    private IHandle<IGpuTexture>? m_Occlusion;
    private IHandle<IGpuTexture>? m_Translucency;

    private readonly SpecularRenderPass m_SpecularRenderPass;

    public Ship(SpecularRenderPass specularRenderPass,
        IHandle<IGpuMesh> mesh, 
        IHandle<IGpuTexture> diffuse, 
        IHandle<IGpuTexture> normal, 
        IHandle<IGpuTexture> roughness, 
        IHandle<IGpuTexture> occlusion, 
        IHandle<IGpuTexture> translucency)
    {
        Transform = new Transform3D();
        m_Mesh = mesh;
        m_SpecularRenderPass = specularRenderPass;
        m_Diffuse = diffuse;
        m_Normal = normal;
        m_Roughness = roughness;
        m_Occlusion = occlusion;
        m_Translucency = translucency;
    }
    
    public void Load(IScene scene)
    {
        m_SpecularRenderPass.Register(new SpecularRenderable
        {
            MeshHandle = m_Mesh,
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

    public void Update(float dt)
    {
        
    }

    public void Unload(IScene scene)
    {
        
    }

    public void Render()
    {
        
    }
}