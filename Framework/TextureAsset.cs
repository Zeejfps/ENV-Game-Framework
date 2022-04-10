namespace TicTacToePrototype;

public class TextureAsset
{
    public int Width { get; init; }
    public int Height { get; init; }
    public string Texture { get; init; } = string.Empty;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Width);
        writer.Write(Height);
        writer.Write(Texture);
    }
    
    public static TextureAsset Deserialize(BinaryReader reader)
    {
        var width = reader.ReadInt32();
        var height = reader.ReadInt32();
        var texture = reader.ReadString();
        
        return new TextureAsset
        {
            Width = width,
            Height = height,
            Texture = texture,
        };
    }
}