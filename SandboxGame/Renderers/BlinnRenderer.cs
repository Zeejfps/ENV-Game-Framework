using System.Diagnostics;
using System.Numerics;
using ENV.Engine;

namespace ENV;

public struct BlinnRenderData
{
    public IMesh Mesh { get; init; }
    public ITransform Transform { get; init; }
}

public class BlinnRenderer : ISceneObject
{
    private IMaterial? m_Material;
    private ITexture? m_Texture;
    private IFramebuffer? m_Framebuffer;
    
    private readonly ICamera m_Camera;
    private readonly ITransform m_Light;
    
    public BlinnRenderer(ICamera camera, ITransform light)
    {
        m_Camera = camera;
        m_Light = light;
    }
    
    public void Load(IScene scene)
    {
        var assetLoader = scene.Context.AssetLoader;
        m_Material = assetLoader.LoadAsset<IMaterial>("Assets/blinn.json");
        m_Texture = assetLoader.LoadAsset<ITexture>("Assets/Textures/uvgrid.texture");

        m_Framebuffer = scene.Context.Window.Framebuffer;
    }

    public void Update(IScene scene)
    {
        
    }

    public void Unload(IScene scene)
    {
        Debug.Assert(m_Material != null);
        m_Material.Unload();
        m_Material = null;
    }

    public void Render(BlinnRenderData renderData)
    {
        var camera = m_Camera;
        var modelMatrix = renderData.Transform.WorldMatrix;
        var framebuffer = m_Framebuffer;
        var mesh = renderData.Mesh;
        var material = m_Material;

        Matrix4x4.Invert(modelMatrix, out var normalMatrix);
        normalMatrix = Matrix4x4.Transpose(normalMatrix);
        
        Debug.Assert(material != null);
        Debug.Assert(framebuffer != null);

        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
        
        material.SetVector3("light_position", m_Light.WorldPosition);
        material.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        material.SetMatrix4x4("matrix_view", viewMatrix);
        material.SetMatrix4x4("matrix_model", modelMatrix);
        material.SetMatrix4x4("normal_matrix", normalMatrix);
        material.SetVector3("camera_position", camera.Transform.WorldPosition);
        framebuffer.RenderMesh(mesh, material);
    }
}