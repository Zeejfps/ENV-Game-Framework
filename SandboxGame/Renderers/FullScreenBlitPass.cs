﻿using System.Drawing;
using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;

namespace Framework;

public class FullScreenBlitPass
{
    public void Render(IGpu gpu, 
        ICamera camera,
        ITransform3D light,
        IHandle<IGpuMesh> quadMeshHandle,
        IHandle<IGpuShader> fullScreenBlitShaderHandle, 
        IGpuTextureHandle bufferAlbedoHandle, 
        IGpuTextureHandle bufferNormalHandle, 
        IGpuTextureHandle bufferPositionHandle)
    {
        gpu.SaveState();
        gpu.EnableBackfaceCulling = true;
        gpu.EnableDepthTest = false;

        var meshManager = gpu.MeshController;
        var shaderManager = gpu.ShaderController;

        shaderManager.Bind(fullScreenBlitShaderHandle);
        meshManager.Bind(quadMeshHandle);
        
        shaderManager.SetTexture2d("gColor", bufferAlbedoHandle);
        shaderManager.SetTexture2d("gNormal", bufferNormalHandle);
        shaderManager.SetTexture2d("gPosition", bufferPositionHandle);
        shaderManager.SetVector3("viewPos", camera.Transform.WorldPosition);
        
        var colors = new[]{Color.Red, Color.Green, Color.Aqua, Color.Gold, Color.Crimson,Color.Lime};
        for (int i = 0; i < 4; i++)
        {
            var newColor = colors[i];
            var convert = .003621f;
            shaderManager.SetVector3($"lights[{i}].Position", light.WorldPosition + new Vector3(i * 10,0,0));
            shaderManager.SetVector3($"lights[{i}].Color", new Vector3(newColor.R * convert, newColor.G * convert, newColor.B * convert));
            shaderManager.SetFloat($"lights[{i}].Power", 15);
        }
        
        meshManager.Render();
        
        gpu.RestoreState();
    }
}