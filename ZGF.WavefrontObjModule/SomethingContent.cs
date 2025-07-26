namespace ZGF.WavefrontObjModule;

public sealed class SomethingContent
{
    public required VertexPosition[] VertexPositions { get; init; }
    public required VertexNormal[] VertexNormals { get; init; }
    public required VertexTextureCoord[] VertexTextureCoords { get; init; }
    public required Face[] Faces { get; init; }
}