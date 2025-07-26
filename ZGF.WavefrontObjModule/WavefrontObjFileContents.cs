namespace ZGF.WavefrontObjModule;

public sealed class WavefrontObjFileContents : IWavefrontObjFileContents
{
    public IEnumerable<INamedObject> NamedObjects { get; }
    public IEnumerable<IGroup> Groups { get; }
    public IEnumerable<ISmoothingGroup> SmoothingGroups { get; }
    public IReadOnlyList<VertexPosition> AllVertexPositions => _data.VertexPositions;
    public IEnumerable<VertexNormal> AllVertexNormals => _data.VertexNormals;
    public IEnumerable<VertexTextureCoord> AllVertexTextureCoords => _data.VertexTextureCoords;
    public IEnumerable<Face> AllFaces => _data.Faces;

    private readonly SomethingContent _data;

    public WavefrontObjFileContents(SomethingContent data)
    {
        _data = data;
    }
}