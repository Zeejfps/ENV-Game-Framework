using EasyGameFramework;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;

namespace Framework;

public class Ship : ISceneObject
{
    public ITransform3D Transform { get; }
    
    private IHandle<IGpuMesh>? m_Mesh;
    private IGpuTextureHandle? m_Diffuse;
    private IGpuTextureHandle? m_Normal;
    private IGpuTextureHandle? m_Roughness;
    private IGpuTextureHandle? m_Occlusion;
    private IGpuTextureHandle? m_Translucency;

    private readonly SpecularRenderPass m_SpecularRenderPass;

    public Ship(SpecularRenderPass specularRenderPass,
        IHandle<IGpuMesh> mesh, 
        IGpuTextureHandle diffuse, 
        IGpuTextureHandle normal, 
        IGpuTextureHandle roughness, 
        IGpuTextureHandle occlusion, 
        IGpuTextureHandle translucency)
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