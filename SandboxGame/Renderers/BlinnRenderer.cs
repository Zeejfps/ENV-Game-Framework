using System.Diagnostics;
using System.Numerics;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;

namespace Framework;

public struct BlinnRenderData
{
    public IHandle<IGpuMesh> MeshHandle { get; init; }
    public ITransform3D Transform { get; init; }
}

public class BlinnRenderer
{
    private IHandle<IGpuShader> m_Shader;
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
        var gpu = scene.App.Gpu;
        var locator = scene.App.Locator;
        var textureLoader = locator.LocateOrThrow<IAssetLoader<IGpuTexture>>();
        m_Shader = gpu.LoadShader("Assets/Shaders/blinn.shader");
        m_Texture = textureLoader.Load("Assets/Textures/test.texture");

        m_Framebuffer = scene.App.Window.Framebuffer;
    }
    
    public void Render(BlinnRenderData renderData)
    {
        var camera = m_Camera;
        var modelMatrix = renderData.Transform.WorldMatrix;

        Matrix4x4.Invert(modelMatrix, out var normalMatrix);
        normalMatrix = Matrix4x4.Transpose(normalMatrix);
        
        Debug.Assert(m_Shader != null);

        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
        
        using var material = m_Shader.Use();
        using var mesh = renderData.MeshHandle.Use();
        material.SetVector3("Light.position", m_Light.WorldPosition);
        material.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        material.SetMatrix4x4("matrix_view", viewMatrix);
        material.SetMatrix4x4("matrix_model", modelMatrix);
        material.SetMatrix4x4("normal_matrix", normalMatrix);
        material.SetVector3("camera_position", camera.Transform.WorldPosition);
        mesh.Render();
    }
}