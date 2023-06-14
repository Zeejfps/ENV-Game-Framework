using System.Diagnostics;
using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;

namespace Framework;

public struct BlinnRenderData
{
    public IHandle<IGpuMesh> MeshHandle { get; init; }
    public ITransform3D Transform { get; init; }
}

public class BlinnRenderer
{
    private IHandle<IGpuShader> m_Shader;
    private IHandle<IGpuTexture>? m_Texture;

    private readonly IGpu m_Gpu;
    private readonly ICamera m_Camera;
    private readonly ITransform3D m_Light;
    
    public BlinnRenderer(IGpu gpu, ICamera camera, ITransform3D light)
    {
        m_Gpu = gpu;
        m_Camera = camera;
        m_Light = light;
    }
    
    public void Load(IScene scene)
    {
        var gpu = m_Gpu;
        m_Shader = gpu.Shader.Load("Assets/Shaders/blinn.shader");
        m_Texture = gpu.TextureController.Load("Assets/Textures/test.texture");
    }
    
    public void Render(IGpu gpu, BlinnRenderData renderData)
    {
        var meshManager = gpu.Mesh;
        var shaderManager = gpu.Shader;
        
        var camera = m_Camera;
        var modelMatrix = renderData.Transform.WorldMatrix;

        Matrix4x4.Invert(modelMatrix, out var normalMatrix);
        normalMatrix = Matrix4x4.Transpose(normalMatrix);
        
        Debug.Assert(m_Shader != null);

        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
        
        shaderManager.Bind(m_Shader);
        shaderManager.SetVector3("Light.position", m_Light.WorldPosition);
        shaderManager.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        shaderManager.SetMatrix4x4("matrix_view", viewMatrix);
        shaderManager.SetMatrix4x4("matrix_model", modelMatrix);
        shaderManager.SetMatrix4x4("normal_matrix", normalMatrix);
        shaderManager.SetVector3("camera_position", camera.Transform.WorldPosition);
        
        meshManager.Bind(renderData.MeshHandle);
        meshManager.Render();
    }
}