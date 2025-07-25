namespace ZGF.WavefrontObjModule;

public interface IWavefrontObjFileContents
{
    IEnumerable<INamedObject> NamedObjects { get; }
    IEnumerable<IGroup> Groups { get; }
    IEnumerable<ISmoothingGroup> SmoothingGroups { get; }
    
    IEnumerable<VertexPosition> AllVertexPositions { get; }
    IEnumerable<VertexNormal> AllVertexNormals { get; }
    IEnumerable<VertexTextureCoord> AllVertexTextureCoords { get; }
}