namespace ZGF.WavefrontObjModule;

public interface IWavefrontObjFileContents
{
    IReadOnlySet<string> MtlFiles { get; }
    IReadOnlyList<IObject> Objects { get; }
    IReadOnlyList<IGroup> Groups { get; }
    IReadOnlyList<ISmoothingGroup> SmoothingGroups { get; }
    IReadOnlyList<VertexPosition> AllVertexPositions { get; }
    IReadOnlyList<VertexNormal> AllVertexNormals { get; }
    IReadOnlyList<VertexTextureCoord> AllVertexTextureCoords { get; }
    IReadOnlyList<Face> AllFaces { get; }
}