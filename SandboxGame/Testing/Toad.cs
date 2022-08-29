using System.Diagnostics;
using EasyGameFramework;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using TicTacToePrototype;

namespace Framework;

public class Toad : ISceneObject
{
    public ITransform3D Transform { get; }
    
    private IHandle<IGpuMesh>? m_MeshHandle;
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
        var gpu = scene.App.Gpu;
        var locator = scene.App.Locator;
        var textureLoader = locator.LocateOrThrow<IAssetLoader<IGpuTexture>>();
        
        m_MeshHandle = gpu.LoadMesh("Assets/Meshes/Toad.mesh");
        m_Diffuse = textureLoader.Load("Assets/Textures/Toad/Toad_BaseColor.texture");
        m_Normal = textureLoader.Load("Assets/Textures/Toad/Toad_Normal.texture");
        m_Roughness = textureLoader.Load("Assets/Textures/Toad/Toad_Roughness.texture");
        m_Occlusion = textureLoader.Load("Assets/Textures/Toad/Toad_AO.texture");
        m_Translucency = textureLoader.Load("Assets/Textures/Toad/Toad_Translucency.texture");

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

    public void Update(IScene scene)
    {
        Debug.Assert(m_MeshHandle != null);
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