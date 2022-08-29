using EasyGameFramework.API.AssetTypes;
using Framework;

namespace TicTacToePrototype;

public class CpuTexture : ICpuTexture
{
    public int Width { get; set; }
    public int Height { get; set; }
    public byte[] Pixels { get; set; } = Array.Empty<byte>();

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Width);
        writer.Write(Height);
        writer.Write(Pixels.Length);
        writer.Write(Pixels);
    }
    
    public static CpuTexture Deserialize(BinaryReader reader)
    {
        var width = reader.ReadInt32();
        var height = reader.ReadInt32();
        var pixelCount = reader.ReadInt32();
        var pixels = reader.ReadBytes(pixelCount);
        
        return new CpuTexture
        {
            Width = width,
            Height = height,
            Pixels = pixels,
        };
    }

    public void Dispose()
    {
        Width = 0;
        Height = 0;
        Pixels = Array.Empty<byte>();
    }
}