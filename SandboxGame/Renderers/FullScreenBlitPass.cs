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

    public void Render(IGpu gpu, 
        IHandle<IGpuMesh> quadMeshHandle,
        IHandle<IGpuShader> fullScreenBlitShaderHandle, 
        IHandle<IGpuTexture> bufferAlbedoHandle, 
        IHandle<IGpuTexture> bufferNormalHandle, 
        IHandle<IGpuTexture> bufferPositionHandle)
    {
        gpu.SaveState();
        gpu.EnableBackfaceCulling = true;
        gpu.EnableDepthTest = false;

        var shaderManager = gpu.ShaderManager;
        
        shaderManager.UseShader(fullScreenBlitShaderHandle);
        using var mesh = quadMeshHandle.Use();
        
        shaderManager.SetTexture2d("gColor", bufferAlbedoHandle);
        shaderManager.SetTexture2d("gNormal", bufferNormalHandle);
        shaderManager.SetTexture2d("gPosition", bufferPositionHandle);
        shaderManager.SetVector3("viewPos", m_Camera.Transform.WorldPosition);
        
        var colors = new[]{Color.Red, Color.Green, Color.Aqua, Color.Gold, Color.Crimson,Color.Lime};
        for (int i = 0; i < 4; i++)
        {
            var x = new Random(i);
            var newColor = colors[i];
            var convert = .003621f;
            shaderManager.SetVector3($"lights[{i}].Position", m_light.WorldPosition + new Vector3(i * 10,0,0));
            shaderManager.SetVector3($"lights[{i}].Color", new Vector3(newColor.R * convert, newColor.G * convert, newColor.B * convert));
            shaderManager.SetFloat($"lights[{i}].Power", 15);
        }
        
        mesh.Render();
        
        gpu.RestoreState();
    }
}