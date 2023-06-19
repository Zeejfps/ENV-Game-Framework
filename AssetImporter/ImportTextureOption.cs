using System.Runtime.CompilerServices;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using EasyGameFramework.Core;
using NativeFileDialogs.Net;
using StbImageSharp;

namespace AssetImporter;

public class ImportTextureOption
{
    public void Run()
    {
        Console.WriteLine("[Import Texture]");
        
        Console.Write("Image Path: ");
        var pathToTexture = Console.ReadLine();
        pathToTexture = pathToTexture.Replace("\"", "");
        
        SaveTexture(pathToTexture);
    }

    public void RunBatch()
    {
        Console.WriteLine("[Import Texture]");
        
        Console.Write("Image Path: ");
        var pathToTexture = Console.ReadLine();
        pathToTexture = pathToTexture?.Replace("\"", "");
        
        var ext = new List<string>{"png"};
        if (pathToTexture == null) return;
        
        var eFiles = Directory.EnumerateFiles(pathToTexture, "*.*", SearchOption.TopDirectoryOnly)
            .Where(s => ext.Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant()));
        var files = eFiles.ToList();

        foreach (var file in files)
        {
            SaveTexture(file);
        }
    }
    

    public void SaveTexture(string pathToTexture)
    {
        var fileName = Path.GetFileNameWithoutExtension(pathToTexture);
        
        ImageResult imageResult = ImageResult.FromMemory(File.ReadAllBytes(pathToTexture), ColorComponents.RedGreenBlueAlpha);

        var pixelBytes = imageResult.Data;
        var width = imageResult.Width;
        var height = imageResult.Height;
        
        var encoder = new BcEncoder
        {
            OutputOptions =
            {
                GenerateMipMaps = false,
                Quality = CompressionQuality.BestQuality,
                Format = CompressionFormat.Bc7,
            }
        };
        
        var data = encoder.EncodeToRawBytes(pixelBytes, width, height, PixelFormat.Rgba32);

        var asset = new CpuTexture
        {
            Width = width,
            Height = height,
            Pixels = data[0],
        };
    
        Console.Write("Save as: ");
        var saveAsPath = Path.GetDirectoryName(pathToTexture);
        var filename = Path.GetFileNameWithoutExtension(pathToTexture);
        
        var strPath = $@"{saveAsPath}\{filename}.texture";
        using var fileStream = File.Open(strPath, FileMode.OpenOrCreate);
        using var writer = new BinaryWriter(fileStream);
        asset.Serialize(writer);
        Console.WriteLine(strPath);
    }
}
