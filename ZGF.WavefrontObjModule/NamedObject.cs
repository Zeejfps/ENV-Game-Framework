namespace ZGF.WavefrontObjModule;

internal sealed class NamedObject : INamedObject
{
    public required string Name { get; init; }

    public required ReadOnlyMemory<VertexPosition> VertexPositions { get; init; }

    public required ReadOnlyMemory<VertexNormal> VertexNormals { get; init; }

    public required ReadOnlyMemory<VertexTextureCoord> VertexTextureCoords { get; init; }

    public required ReadOnlyMemory<Face> Faces { get; init; }
}