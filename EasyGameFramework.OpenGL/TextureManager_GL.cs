using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;
using EasyGameFramework.Core.AssetLoaders;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

using System;
using System.Diagnostics;

internal class TextureManager_GL : GpuResourceManager<IGpuTextureHandle, ITexture_GL>, ITextureController
{
    private TextureFilterKind Filter { get; set; }

    private readonly CpuTextureAssetLoader m_CpuTextureAssetLoader = new();

    protected override void OnBound(ITexture_GL resource)
    {
        switch (resource)
        {
            case Texture2D_GL:
                glBindTexture(GL_TEXTURE_2D, resource.Id);
                break;
            case CubeMapTexture_GL:
                glBindTexture(GL_TEXTURE_CUBE_MAP, resource.Id);
                break;
        }
    }

    protected override void OnUnbound()
    {
        switch (BoundResource)
        {
            case Texture2D_GL:
                glBindTexture(GL_TEXTURE_2D, 0);
                break;
            case CubeMapTexture_GL:
                glBindTexture(GL_TEXTURE_CUBE_MAP, 0);
                break;
        }
    }

    protected override IGpuTextureHandle CreateHandle(ITexture_GL resource)
    {
        return resource switch
        {
            Texture2D_GL texture2D_GL => new GpuReadonlyTextureHandle(texture2D_GL),
            CubeMapTexture_GL cubeMapTexture_GL => new GpuReadonlyCubeMapHandle(cubeMapTexture_GL),
            _ => null
        };
    }



    private ITexture_GL LoadAndBindTexture2D(string assetPath)
    {
        var asset = m_CpuTextureAssetLoader.Load(assetPath);
        var width = asset.Width;
        var height = asset.Height;
        var pixels = asset.Pixels;
        var texture = ReadonlyTexture2D_GL.Create(width, height, pixels, Filter);
        return texture;
    }
    
    private ITexture_GL LoadAndBindCubeMap(string[] assetPaths)
    {
        if (assetPaths.Length != 6)
            throw new ArgumentException("A cube map requires 6 textures.");

        var assets = new ICpuTexture[6];
        for (var i = 0; i < 6; i++)
        {
            assets[i] = m_CpuTextureAssetLoader.Load(assetPaths[i]);
        }

        var width = assets[0].Width;
        var height = assets[0].Height;
        var facesData = new byte[6][];

        for (var i = 0; i < 6; i++)
        {
            facesData[i] = assets[i].Pixels;
        }

        var texture = ReadonlyCubeMap_GL.Create(width, height, facesData, Filter);
        return texture;
    }


    // public IGpuTextureHandle Load(string assetPath, TextureFilterKind filter)
    // {
    //     Filter = filter;
    //     return Load(assetPath);
    // }

    public IGpuTextureHandle Load(string assetPath, TextureFilterKind filter, TextureKind textureType = TextureKind.Texture2D)
    {
        Filter = filter;
        IGpuTextureHandle refHandle = null!;
        switch (textureType)
        {
            case TextureKind.Texture2D:
                LoadTexture2D(assetPath, ref refHandle);
                return refHandle;
            case TextureKind.CubeMap:
                LoadCubeMap(new[] { assetPath }, ref refHandle);
                return refHandle;
            default:
                throw new NotImplementedException($"Loading of texture type {textureType} is not implemented.");
        }
    }

    private ITexture_GL LoadTexture2D(string assetPath, ref IGpuTextureHandle refHandle)
    {
        ITexture_GL texture = LoadAndBindTexture2D(assetPath);
        GpuReadonlyTextureHandle handle = new GpuReadonlyTextureHandle((Texture2D_GL)texture);
        Add(handle, texture);
        refHandle = handle;
        return texture;
    }

    private ITexture_GL LoadCubeMap(string[] assetPaths, ref IGpuTextureHandle refHandle)
    {
        ITexture_GL texture = LoadAndBindCubeMap(assetPaths);
        GpuReadonlyCubeMapHandle handle = new GpuReadonlyCubeMapHandle((CubeMapTexture_GL)texture);
        Add(handle, texture);
        refHandle = handle;
        return texture;
    }
    protected override ITexture_GL LoadAndBindResource(string assetPath)
    {
        Console.WriteLine("Warning: LoadAndBindResource is deprecated. Use LoadTexture2D or LoadCubeMap instead.");
        
        IGpuTextureHandle? refHandle = null;
        return LoadTexture2D(assetPath, ref refHandle);
    }

    public void SaveState()
    {
    }

    public void RestoreState()
    {
    }

    public void Upload(ReadOnlySpan<byte> pixels)
    {
        Debug.Assert(BoundResource != null);
        BoundResource.Upload(pixels);
    }
}
