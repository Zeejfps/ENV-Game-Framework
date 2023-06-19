public interface ITexture_GL
{
    uint Id { get; }
    int Width { get; }
    int Height { get; }
    void Upload(ReadOnlySpan<byte> pixels, int? faceIndex = null);
}