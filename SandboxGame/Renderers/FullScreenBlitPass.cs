using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;

namespace Framework;

public class FullScreenBlitPass
{
    private readonly ICamera m_Camera;
    private readonly ITransform3D m_light;

    public FullScreenBlitPass(ICamera camera, ITransform3D light)
    {
        m_Camera = camera;
        m_light = light;
    }

    public void Render(IGpuMesh quadMesh, IGpu gpu, IHandle<IGpuShader> fullScreenBlitMaterial, IGpuTexture bufferAlbedo, IGpuTexture bufferNormal, IGpuTexture bufferPosition)
    {
        gpu.SaveState();
        gpu.EnableBackfaceCulling = true;
        gpu.EnableDepthTest = false;
        
        using var shader = fullScreenBlitMaterial.Use();
        using var mesh = quadMesh.Use();
        
        shader.SetTexture2d("gColor", bufferAlbedo);
        shader.SetTexture2d("gNormal", bufferNormal);
        shader.SetTexture2d("gPosition", bufferPosition);
        shader.SetVector3("viewPos", m_Camera.Transform.WorldPosition);
        
        var colors = new[]{Color.Red, Color.Green, Color.Aqua, Color.Gold, Color.Crimson,Color.Lime};
        for (int i = 0; i < 4; i++)
        {
            var x = new Random(i);
            var newColor = colors[i];
            var convert = .003621f;
            shader.SetVector3($"lights[{i}].Position", m_light.WorldPosition + new Vector3(i * 10,0,0));
            shader.SetVector3($"lights[{i}].Color", new Vector3(newColor.R * convert, newColor.G * convert, newColor.B * convert));
            shader.SetFloat($"lights[{i}].Power", 15);
        }
        
        mesh.Render();
        
        gpu.RestoreState();
    }
}