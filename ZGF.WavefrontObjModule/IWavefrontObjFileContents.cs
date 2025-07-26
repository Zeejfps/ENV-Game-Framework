namespace ZGF.WavefrontObjModule;

public interface IWavefrontObjFileContents
{
    IReadOnlyList<string> Comments { get; }
    IReadOnlySet<string> MtlFiles { get; }
    IReadOnlyList<IObject> Objects { get; }
    IReadOnlyList<IGroup> Groups { get; }
    IReadOnlyList<ISmoothingGroup> SmoothingGroups { get; }
    ReadOnlyMemory<VertexPosition> VertexPositions { get; }
    ReadOnlyMemory<VertexNormal> VertexNormals { get; }
    ReadOnlyMemory<VertexTextureCoord> VertexTextureCoords { get; }
    ReadOnlyMemory<Face> Faces { get; }
}