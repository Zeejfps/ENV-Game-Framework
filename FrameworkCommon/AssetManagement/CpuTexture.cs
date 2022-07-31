using Framework;

namespace TicTacToePrototype;

public class CpuTexture : ICpuTexture
{
    public int Width { get; init; }
    public int Height { get; init; }
    public byte[] Pixels { get; init; } = Array.Empty<byte>();

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
        
    }
}