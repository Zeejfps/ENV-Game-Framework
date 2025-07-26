namespace ZGF.WavefrontObjModule;

public interface IObject
{
    ReadOnlyMemory<VertexPosition> VertexPositions { get; }
    ReadOnlyMemory<VertexNormal> VertexNormals { get; }
    ReadOnlyMemory<VertexTextureCoord> VertexTextureCoords { get; }
    ReadOnlyMemory<Face> Faces { get; }
}