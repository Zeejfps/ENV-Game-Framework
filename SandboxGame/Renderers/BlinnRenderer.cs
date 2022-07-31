using System.Diagnostics;
using System.Numerics;
using Framework;

namespace Framework;

public struct BlinnRenderData
{
    public IGpuMesh Mesh { get; init; }
    public ITransform3D Transform { get; init; }
}

public class BlinnRenderer
{
    private IGpuShader? m_Material;
    private IGpuTexture? m_Texture;
    private IGpuFramebuffer? m_Framebuffer;
    
    private readonly ICamera m_Camera;
    private readonly ITransform3D m_Light;
    
    public BlinnRenderer(ICamera camera, ITransform3D light)
    {
        m_Camera = camera;
        m_Light = light;
    }
    
    public void Load(IScene scene)
    {
        var assetLoader = scene.Context.AssetService;
        m_Material = assetLoader.Load<IGpuShader>("Assets/blinn.json");
        m_Texture = assetLoader.Load<IGpuTexture>("Assets/Textures/test.texture");

        m_Framebuffer = scene.Context.Window.Framebuffer;
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

        Matrix4x4.Invert(modelMatrix, out var normalMatrix);
        normalMatrix = Matrix4x4.Transpose(normalMatrix);
        
        Debug.Assert(m_Material != null);

        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
        
        using var material = m_Material.Use();
        using var mesh = renderData.Mesh.Use();
        material.SetVector3("Light.position", m_Light.WorldPosition);
        material.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        material.SetMatrix4x4("matrix_view", viewMatrix);
        material.SetMatrix4x4("matrix_model", modelMatrix);
        material.SetMatrix4x4("normal_matrix", normalMatrix);
        material.SetVector3("camera_position", camera.Transform.WorldPosition);
        mesh.Render();
    }
}