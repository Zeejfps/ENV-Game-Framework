namespace ZGF.WavefrontObjModule;

public interface IWavefrontObjFileContents
{
    IReadOnlySet<string> MtlFiles { get; }
    IReadOnlyList<IObject> Objects { get; }
    IReadOnlyList<IGroup> Groups { get; }
    IReadOnlyList<ISmoothingGroup> SmoothingGroups { get; }
    IReadOnlyList<VertexPosition> VertexPositions { get; }
    IReadOnlyList<VertexNormal> VertexNormals { get; }
    IReadOnlyList<VertexTextureCoord> VertexTextureCoords { get; }
    IReadOnlyList<Face> Faces { get; }
}