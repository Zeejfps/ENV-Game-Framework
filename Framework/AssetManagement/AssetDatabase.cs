﻿using System.Runtime.Serialization.Formatters.Binary;
using Framework;
using TicTacToePrototype;

namespace Framework;

public interface IAssetLoaderModule
{
    IAsset LoadAsset(string assetPath);
}

public interface IAssetLoader<T> : IAssetLoaderModule where T : IAsset
{
    
}

public class AssetDatabase : IAssetDatabase
{
    private Dictionary<Type, IAssetLoaderModule> m_TypeToModuleMap = new();

    public T LoadAsset<T>(string assetPath) where T : IAsset
    {
        var assetType = typeof(T);
        if (m_TypeToModuleMap.TryGetValue(assetType, out var module))
            return (T)module.LoadAsset(assetPath);

        throw new Exception($"Could not find Module for asset type: {assetType}");
    }

    public void AddLoader<T>(IAssetLoader<T> assetLoaderModule) where T : IAsset
    {
        var assetType = typeof(T);
        m_TypeToModuleMap[assetType] = assetLoaderModule;
    }
}

public class MeshAssetLoaderModule : IAssetLoader<IMesh>
{
    private readonly Dictionary<string, IMesh> m_LoadedAssets = new();

    public IAsset LoadAsset(string assetPath)
    {
        var fileExtension = Path.GetExtension(assetPath);
        if (fileExtension != ".obj")
            throw new Exception($"Invalid Asset Extension: {fileExtension}");

        if (m_LoadedAssets.TryGetValue(assetPath, out var asset) && asset.IsLoaded)
            return asset;

        asset = OBJLoader.LoadObjFromFile(assetPath);
        m_LoadedAssets[assetPath] = asset;
        return asset;
    }
}

public abstract class TextureAssetLoaderModule : IAssetLoader<ITexture>
{
    public IAsset LoadAsset(string assetPath)
    {
        if (!File.Exists(assetPath))
            throw new Exception($"File does not exists {assetPath}");

        using var stream = File.Open(assetPath, FileMode.Open);
        using var reader = new BinaryReader(stream);

        var asset = TextureAsset_GL.Deserialize(reader);
        return LoadAsset(asset);
    }

    protected abstract ITexture LoadAsset(TextureAsset_GL asset);
}