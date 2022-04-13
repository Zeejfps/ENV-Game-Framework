using System.Diagnostics;

namespace Framework;

public class FullScreenBlitPass
{
    private IMaterial? m_FullScreenBlitMaterial;
    private IMesh? m_QuadMesh;
    
    public void Load(IContext context)
    {
        var assetDatabase = context.AssetDatabase;
        m_FullScreenBlitMaterial = assetDatabase.LoadAsset<IMaterial>("Assets/Materials/fullScreenQuad.material");
        m_FullScreenBlitMaterial.UseBackfaceCulling = true;
        m_FullScreenBlitMaterial.UseDepthTest = false;
        m_QuadMesh = assetDatabase.LoadAsset<IMesh>("Assets/Meshes/quad.mesh");
    }
    
    public void Render(ITexture screenTexture)
    {
        Debug.Assert(m_FullScreenBlitMaterial != null);
        Debug.Assert(m_QuadMesh != null);
        
        using var material = m_FullScreenBlitMaterial.Use();
        material.SetTexture2d("screenTexture", screenTexture);
        m_QuadMesh.Render();
    }
}