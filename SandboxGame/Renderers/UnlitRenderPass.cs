using System.Numerics;
using Framework;

namespace Framework;

public class UnlitRendererable
{
    public IMesh Mesh { get; init; }
    public Vector3 Color { get; init; }
    public ITransform Transform { get; init; }
}

public class UnlitRenderPass
{
    private IMaterial m_Material;

    private readonly ICamera m_Camera;

    private List<UnlitRendererable> m_Rendererables = new List<UnlitRendererable>();

    public UnlitRenderPass(ICamera camera)
    {
        m_Camera = camera;
    }
    
    public void Load(IScene scene)
    {
        m_Material = scene.Context.AssetDatabase.LoadAsset<IMaterial>("Assets/Materials/unlit.material");
        m_Material.UseDepthTest = true;
        m_Material.UseBackfaceCulling = true;
    }

    public void Add(UnlitRendererable rendererable)
    {
        m_Rendererables.Add(rendererable);
    }

    public void Render()
    {
        var camera = m_Camera;
        using var material = m_Material.Use();
        material.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);

        foreach (var data in m_Rendererables)
        {
            var modelMatrix = data.Transform.WorldMatrix;
            Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
        
            material.SetMatrix4x4("matrix_view", viewMatrix);
            material.SetMatrix4x4("matrix_model", modelMatrix);
            material.SetVector3("color", data.Color);
            data.Mesh.Render();
        }
    }

    public void Unload(IScene scene)
    {
    }
}