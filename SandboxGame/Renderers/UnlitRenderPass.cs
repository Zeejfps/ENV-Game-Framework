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

    public void Render(IGpu gpu, IHandle<IGpuShader> material, ICamera camera)
    {
        gpu.SaveState();
        gpu.EnableDepthTest = true;
        gpu.EnableBackfaceCulling = false;
     
        using var materialHandle = material.Use();
        materialHandle.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);

        foreach (var data in m_Renderables)
        {
            var modelMatrix = data.Transform.WorldMatrix;
            Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
        
            materialHandle.SetMatrix4x4("matrix_view", viewMatrix);
            materialHandle.SetMatrix4x4("matrix_model", modelMatrix);
            materialHandle.SetVector3("color", data.Color);

            using var mesh = data.MeshHandle.Use();
            mesh.Render();
        }
        
        gpu.RestoreState();
    }
}