using System.Diagnostics;
using System.Numerics;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using Framework.Common;
using TicTacToePrototype;

namespace Framework;

public class Ship : ISceneObject
{
    public ITransform3D Transform { get; }
    
    private IGpuMesh? m_Mesh;
    private IGpuTexture? m_Diffuse;
    private IGpuTexture? m_Normal;
    private IGpuTexture? m_Roughness;
    private IGpuTexture? m_Occlusion;
    private IGpuTexture? m_Translucency;

    private readonly SpecularRenderPass m_SpecularRenderPass;

    public Ship(SpecularRenderPass specularRenderPass,
        IGpuMesh mesh, 
        IGpuTexture diffuse, 
        IGpuTexture normal, 
        IGpuTexture roughness, 
        IGpuTexture occlusion, 
        IGpuTexture translucency)
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