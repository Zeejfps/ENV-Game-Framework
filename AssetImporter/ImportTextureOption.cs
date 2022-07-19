using System.Runtime.CompilerServices;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using NativeFileDialogs.Net;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TicTacToePrototype;

namespace AssetImporter;

public class ImportTextureOption
{
    public void Run()
    {
        Console.WriteLine("[Import Texture]");
        // Console.WriteLine("1 - Set Image Path");
        // Console.WriteLine("2 - Set Output Path");
        // Console.Write("Option: ");
        // Console.ReadLine();
        
        Console.Write("Image Path: ");
        var pathToTexture = Console.ReadLine();
        pathToTexture = pathToTexture.Replace("\"", "");
        
        SaveTexture(pathToTexture);
    }

    public void RunBatch()
    {
        Console.WriteLine("[Import Texture]");
        // Console.WriteLine("1 - Set Image Path");
        // Console.WriteLine("2 - Set Output Path");
        // Console.Write("Option: ");
        // Console.ReadLine();
        
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
        using var image = Image.Load<Rgba32>(pathToTexture);

        var pixelBytes = new byte[image.Width * image.Height * Unsafe.SizeOf<Rgba32>()];
        image.CopyPixelDataTo(pixelBytes);

        var width = image.Width;
        var height = image.Height;
        
        var encoder = new BcEncoder
        {
            OutputOptions =
            {
                GenerateMipMaps = false,
                Quality = CompressionQuality.BestQuality,
                Format = CompressionFormat.Bc7,
                //FileFormat = OutputFileFormat.Ktx //Change to Dds for a dds
            }
        };
        
        var data = encoder.EncodeToRawBytes(pixelBytes, image.Width, image.Height, PixelFormat.Rgba32);

        var asset = new TextureAsset
        {
            Width = width,
            Height = height,
            Pixels = data[0],
        };
    
        Console.Write("Save as: ");
        var saveAsPath = Path.GetDirectoryName(pathToTexture);
        var filename = Path.GetFileNameWithoutExtension(pathToTexture);
        
        //saveAsPath = saveAsPath.Replace("\"", "");
        var strPath = $@"{saveAsPath}\{filename}.texture";
        using var fileStream = File.Open(strPath, FileMode.OpenOrCreate);
        using var writer = new BinaryWriter(fileStream);
        asset.Serialize(writer);
        Console.WriteLine(strPath);
    }
}