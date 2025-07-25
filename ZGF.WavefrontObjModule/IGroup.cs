namespace ZGF.WavefrontObjModule;

public interface IGroup {
    ReadOnlySpan<VertexPosition> VertexPositions { get; }
    ReadOnlySpan<VertexNormal> VertexNormals { get; }
    ReadOnlySpan<VertexTextureCoord> VertexTextureCoords { get; }
    ReadOnlySpan<Face> Faces { get; }
}