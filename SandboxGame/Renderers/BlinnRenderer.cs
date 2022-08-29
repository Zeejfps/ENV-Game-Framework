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
    private IHandle<IGpuTexture>? m_Texture;
    
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
        m_Shader = gpu.LoadShader("Assets/Shaders/blinn.shader");
        m_Texture = gpu.LoadTexture("Assets/Textures/test.texture");
    }
    
    public void Render(IGpu gpu, BlinnRenderData renderData)
    {
        var meshManager = gpu.MeshManager;
        var shaderManager = gpu.ShaderManager;
        
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