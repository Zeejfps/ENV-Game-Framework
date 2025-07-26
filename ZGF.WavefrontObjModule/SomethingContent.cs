namespace ZGF.WavefrontObjModule;

internal sealed class SomethingContent
{
    public required NamedObject[] NamedObjects { get; init; }
    public required VertexPosition[] VertexPositions { get; init; }
    public required VertexNormal[] VertexNormals { get; init; }
    public required VertexTextureCoord[] VertexTextureCoords { get; init; }
    public required Face[] Faces { get; init; }
}