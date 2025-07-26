namespace ZGF.WavefrontObjModule;

public interface IWavefrontObjFileContents
{
    IEnumerable<IObject> NamedObjects { get; }
    IEnumerable<IGroup> Groups { get; }
    IEnumerable<ISmoothingGroup> SmoothingGroups { get; }
    
    IReadOnlyList<VertexPosition> AllVertexPositions { get; }
    IEnumerable<VertexNormal> AllVertexNormals { get; }
    IEnumerable<VertexTextureCoord> AllVertexTextureCoords { get; }
}