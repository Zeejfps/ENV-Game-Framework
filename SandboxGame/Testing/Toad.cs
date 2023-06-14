using EasyGameFramework;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;
using Framework.Materials;

namespace Framework;

public class Toad : ISceneObject
{
    public ITransform3D Transform { get; }
    
    private IHandle<IGpuMesh>? m_MeshHandle;
    private IGpuTextureHandle? m_Diffuse;
    private IGpuTextureHandle? m_Normal;
    private IGpuTextureHandle? m_Roughness;
    private IGpuTextureHandle? m_Occlusion;
    private IGpuTextureHandle? m_Translucency;

    private UnlitMaterial m_Material;

    private IGpu Gpu { get; }
    private readonly SpecularRenderPass m_SpecularRenderPass;

    public Toad(IGpu gpu, SpecularRenderPass specularRenderPass)
    {
        Gpu = gpu;
        Transform = new Transform3D();
        m_SpecularRenderPass = specularRenderPass;
    }
    
    public void Load(IScene scene)
    {
        var gpu = Gpu;
        
        m_MeshHandle = gpu.MeshController.Load("Assets/Meshes/Toad");
        m_Diffuse = gpu.TextureController.Load("Assets/Textures/Toad/Toad_BaseColor");
        m_Normal = gpu.TextureController.Load("Assets/Textures/Toad/Toad_Normal");
        m_Roughness = gpu.TextureController.Load("Assets/Textures/Toad/Toad_Roughness");
        m_Occlusion = gpu.TextureController.Load("Assets/Textures/Toad/Toad_AO");
        m_Translucency = gpu.TextureController.Load("Assets/Textures/Toad/Toad_Translucency");

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