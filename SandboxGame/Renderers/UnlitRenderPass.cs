using System.Numerics;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;

namespace Framework;

public class UnlitRendererable
{
    public IHandle<IGpuMesh> MeshHandle { get; init; }
    public Vector3 Color { get; init; }
    public ITransform3D Transform { get; init; }
}

public class UnlitRenderPass
{
    private List<UnlitRendererable> m_Renderables = new();

    public void Add(UnlitRendererable rendererable)
    {
        m_Renderables.Add(rendererable);
    }

    public void Render(IGpu gpu, IHandle<IGpuShader> shader, ICamera camera)
    {
        gpu.SaveState();
        gpu.EnableDepthTest = true;
        gpu.EnableBackfaceCulling = false;

        var meshManager = gpu.MeshManager;
        var shaderManager = gpu.ShaderManager;
        shaderManager.Use(shader);
        shaderManager.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);

        foreach (var data in m_Renderables)
        {
            var modelMatrix = data.Transform.WorldMatrix;
            Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
        
            shaderManager.SetMatrix4x4("matrix_view", viewMatrix);
            shaderManager.SetMatrix4x4("matrix_model", modelMatrix);
            shaderManager.SetVector3("color", data.Color);

            meshManager.Use(data.MeshHandle);
            meshManager.Render();
        }
        
        gpu.RestoreState();
    }
}