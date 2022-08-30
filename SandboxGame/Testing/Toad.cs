using EasyGameFramework;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using Framework.Materials;

namespace Framework;

public class Toad : ISceneObject
{
    public ITransform3D Transform { get; }
    
    private IHandle<IGpuMesh>? m_MeshHandle;
    private IHandle<IGpuTexture>? m_Diffuse;
    private IHandle<IGpuTexture>? m_Normal;
    private IHandle<IGpuTexture>? m_Roughness;
    private IHandle<IGpuTexture>? m_Occlusion;
    private IHandle<IGpuTexture>? m_Translucency;

    private UnlitMaterial m_Material;

    private readonly SpecularRenderPass m_SpecularRenderPass;

    public Toad(SpecularRenderPass specularRenderPass)
    {
        Transform = new Transform3D();
        m_SpecularRenderPass = specularRenderPass;
    }
    
    public void Load(IScene scene)
    {
        var gpu = scene.Context.Gpu;
        
        m_MeshHandle = gpu.Mesh.Load("Assets/Meshes/Toad.mesh");
        m_Diffuse = gpu.Texture.Load("Assets/Textures/Toad/Toad_BaseColor.texture");
        m_Normal = gpu.Texture.Load("Assets/Textures/Toad/Toad_Normal.texture");
        m_Roughness = gpu.Texture.Load("Assets/Textures/Toad/Toad_Roughness.texture");
        m_Occlusion = gpu.Texture.Load("Assets/Textures/Toad/Toad_AO.texture");
        m_Translucency = gpu.Texture.Load("Assets/Textures/Toad/Toad_Translucency.texture");

        m_SpecularRenderPass.Register(new SpecularRenderable
        {
            Transform = Transform,
            MeshHandle = m_MeshHandle,
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
        // m_Material.Batch(m_MeshHandle, new UnlitMaterial.Properties
        // {
        //     ModelMatrix = Transform.WorldMatrix,
        // });
    }
}