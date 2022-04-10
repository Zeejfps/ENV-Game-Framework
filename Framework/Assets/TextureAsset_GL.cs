namespace TicTacToePrototype;

public class TextureAsset_GL
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
    
    public static TextureAsset_GL Deserialize(BinaryReader reader)
    {
        var width = reader.ReadInt32();
        var height = reader.ReadInt32();
        var byteCount = reader.ReadInt32();
        var texture = reader.ReadBytes(byteCount);
        
        return new TextureAsset_GL
        {
            Width = width,
            Height = height,
            Pixels = texture,
        };
    }
}