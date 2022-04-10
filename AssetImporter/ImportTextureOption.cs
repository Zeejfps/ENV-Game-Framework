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
                Quality = CompressionQuality.Balanced,
                Format = CompressionFormat.Bc7,
                //FileFormat = OutputFileFormat.Ktx //Change to Dds for a dds
            }
        };

        var data = encoder.EncodeToRawBytes(pixelBytes, image.Width, image.Height, PixelFormat.Rgba32);

        var asset = new TextureAsset_GL
        {
            Width = width,
            Height = height,
            Pixels = data[0],
        };
    
        Console.Write("Save as: ");
        var saveAsPath = Console.ReadLine();
        
        saveAsPath = saveAsPath.Replace("\"", "");

        using var fileStream = File.Open(saveAsPath, FileMode.OpenOrCreate);
        using var writer = new BinaryWriter(fileStream);
        asset.Serialize(writer);
    }
}