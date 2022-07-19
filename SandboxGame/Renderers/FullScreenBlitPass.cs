using System.Diagnostics;
using System.Drawing;
using System.Numerics;

namespace Framework;

public class FullScreenBlitPass
{
    private IMaterial? m_FullScreenBlitMaterial;
    private readonly ICamera m_Camera;
    private readonly ITransform3D m_light;

    public FullScreenBlitPass(ICamera camera, ITransform3D light)
    {
        m_Camera = camera;
        m_light = light;
    }

    public void Load(IContext context)
    {
        var assetDatabase = context.AssetDatabase;
        m_FullScreenBlitMaterial = assetDatabase.LoadAsset<IMaterial>("Assets/Materials/fullScreenQuad.material");
        m_FullScreenBlitMaterial.EnableBackfaceCulling = true;
        m_FullScreenBlitMaterial.EnableDepthTest = false;
    }
    
    public void Render(IMesh quadMesh, ITexture bufferAlbedo, ITexture bufferNormal, ITexture bufferPosition)
    {
        Debug.Assert(m_FullScreenBlitMaterial != null);
        Debug.Assert(quadMesh != null);
        
        using var material = m_FullScreenBlitMaterial.Use();
        using var mesh = quadMesh.Use();
        
        material.SetTexture2d("gColor", bufferAlbedo);
        material.SetTexture2d("gNormal", bufferNormal);
        material.SetTexture2d("gPosition", bufferPosition);
        material.SetVector3("viewPos", m_Camera.Transform.WorldPosition);
        
        var colors = new[]{Color.Red, Color.Green, Color.Aqua, Color.Gold, Color.Crimson,Color.Lime};
        for (int i = 0; i < 4; i++)
        {
            var x = new Random(i);
            var newColor = colors[i];
            var convert = .003621f;
            material.SetVector3($"lights[{i}].Position", m_light.WorldPosition + new Vector3(i * 10,0,0));
            material.SetVector3($"lights[{i}].Color", new Vector3(newColor.R * convert, newColor.G * convert, newColor.B * convert));
            material.SetFloat($"lights[{i}].Power", 15);
        }
        

        mesh.Render();
    }
}