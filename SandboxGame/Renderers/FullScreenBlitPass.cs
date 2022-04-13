using System.Diagnostics;
using System.Numerics;

namespace Framework;

public class FullScreenBlitPass
{
    private IMaterial? m_FullScreenBlitMaterial;
    private IMesh? m_QuadMesh;
    private readonly ICamera m_Camera;
    private readonly ITransform m_light;

    public FullScreenBlitPass(ICamera camera, ITransform light)
    {
        m_Camera = camera;
        m_light = light;
    }

    public void Load(IContext context)U
    {
        var assetDatabase = context.AssetDatabase;
        m_FullScreenBlitMaterial = assetDatabase.LoadAsset<IMaterial>("Assets/Materials/fullScreenQuad.material");
        m_FullScreenBlitMaterial.UseBackfaceCulling = true;
        m_FullScreenBlitMaterial.UseDepthTest = false;
        m_QuadMesh = assetDatabase.LoadAsset<IMesh>("Assets/Meshes/quad.mesh");
    }
    
    public void Render(ITexture bufferAlbedo, ITexture bufferNormal, ITexture bufferPosition)
    {
        Debug.Assert(m_FullScreenBlitMaterial != null);
        Debug.Assert(m_QuadMesh != null);
        
        using var material = m_FullScreenBlitMaterial.Use();
        using var mesh = m_QuadMesh.Use();
        
        material.SetTexture2d("gColor", bufferAlbedo);
        material.SetTexture2d("gNormal", bufferNormal);
        material.SetTexture2d("gPosition", bufferPosition);
        material.SetVector3("viewPos", m_Camera.Transform.WorldPosition);
        material.SetVector3("lights[0].Position", m_light.WorldPosition);
        material.SetVector3("lights[0].Color", new Vector3(1,1,1));
        material.SetFloat("lights[0].Power", 5);

        mesh.Render();
    }
}