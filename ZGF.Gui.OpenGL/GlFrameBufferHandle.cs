namespace ZGF.Gui.OpenGL;

public readonly struct GlFrameBufferHandle
{
    public required uint FrameBufferId { get; init; }
    public required uint ColorTextureId { get; init; }
    public required string ImageId { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
}