namespace ZGF.WavefrontObjModule;

public interface INamedObject
{
    ReadOnlyMemory<VertexPosition> VertexPositions { get; }
    ReadOnlyMemory<VertexNormal> VertexNormals { get; }
    ReadOnlyMemory<VertexTextureCoord> VertexTextureCoords { get; }
    ReadOnlyMemory<Face> Faces { get; }
}