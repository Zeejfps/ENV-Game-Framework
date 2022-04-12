using System.Numerics;
using Framework;

namespace Framework;

public struct UnlitRenderData
{
    public IMesh Mesh { get; init; }
    public Vector3 Color { get; init; }
    public ITransform Transform { get; init; }
}

public class UnlitRenderer : ISceneObject
{
    private IMaterial m_Material;
    private IFramebuffer m_Framebuffer;

    private readonly ICamera m_Camera;
    
    public UnlitRenderer(ICamera camera)
    {
        m_Camera = camera;
    }
    
    public void Load(IScene scene)
    {
        m_Material = scene.Context.AssetDatabase.LoadAsset<IMaterial>("Assets/Materials/unlit.material");
        m_Framebuffer = scene.Context.Window.Framebuffer;
    }

    public void Update(IScene scene)
    {
    }

    public void Unload(IScene scene)
    {
    }

    public void Render(UnlitRenderData data)
    {
        var camera = m_Camera;
        
        var modelMatrix = data.Transform.WorldMatrix;
        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
        
        var material = m_Material.Use();
        material.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        material.SetMatrix4x4("matrix_view", viewMatrix);
        material.SetMatrix4x4("matrix_model", modelMatrix);
        material.SetVector3("color", data.Color);
        data.Mesh.Render();
    }
}