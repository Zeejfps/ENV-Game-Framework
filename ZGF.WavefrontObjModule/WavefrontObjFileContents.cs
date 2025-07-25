namespace ZGF.WavefrontObjModule;

public sealed class WavefrontObjFileContents : IWavefrontObjFileContents
{
    public IEnumerable<INamedObject> NamedObjects { get; }
    public IEnumerable<IGroup> Groups { get; }
    public IEnumerable<ISmoothingGroup> SmoothingGroups { get; }
    public IReadOnlyList<VertexPosition> AllVertexPositions { get; }
    public IEnumerable<VertexNormal> AllVertexNormals { get; }
    public IEnumerable<VertexTextureCoord> AllVertexTextureCoords { get; }

    public VertexPosition[] vertexPositions;
    public VertexNormal[] vertexNormals;
    public VertexTextureCoord[] vertexTextureCoords;
    public Face[] faces;
    
    public WavefrontObjFileContents()
    {
               
    }
}