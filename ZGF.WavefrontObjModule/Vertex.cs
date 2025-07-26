namespace ZGF.WavefrontObjModule;

public readonly struct Vertex
{
    public required int PositionIndex { get; init; }
    public required int TextureCoordIndex { get; init; }
    public required int NormalIndex { get; init; }
}