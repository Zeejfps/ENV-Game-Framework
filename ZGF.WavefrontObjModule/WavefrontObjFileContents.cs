namespace ZGF.WavefrontObjModule;

internal sealed class WavefrontObjFileContents : IWavefrontObjFileContents
{
    public IEnumerable<IObject> NamedObjects { get; }
    public IEnumerable<IGroup> Groups { get; }
    public IEnumerable<ISmoothingGroup> SmoothingGroups { get; }
    public IReadOnlyList<VertexPosition> AllVertexPositions => _data.VertexPositions;
    public IEnumerable<VertexNormal> AllVertexNormals => _data.VertexNormals;
    public IEnumerable<VertexTextureCoord> AllVertexTextureCoords => _data.VertexTextureCoords;
    public IEnumerable<Face> AllFaces => _data.Faces;

    private readonly RawModelData _data;

    public WavefrontObjFileContents(RawModelData data)
    {
        _data = data;
    }
}