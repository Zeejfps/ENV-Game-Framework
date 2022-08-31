using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Rendering;
using Framework.Materials;

namespace Framework;

public class UnlitRenderPass
{
    private readonly UnlitMaterial m_Material;
    
    public UnlitRenderPass(UnlitMaterial material)
    {
        m_Material = material;
    }
    
    public void Render(IGpu gpu, ICamera camera)
    {
        m_Material.ProjectionMatrix = camera.ProjectionMatrix;
        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
        m_Material.ViewMatrix = viewMatrix;
        m_Material.RenderBatches(gpu);
    }
}