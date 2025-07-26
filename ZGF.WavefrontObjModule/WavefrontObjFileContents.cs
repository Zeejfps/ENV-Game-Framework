namespace ZGF.WavefrontObjModule;

internal sealed class WavefrontObjFileContents : IWavefrontObjFileContents
{
    public IReadOnlySet<string> MtlFiles => _data.MtlFiles;
    public IReadOnlyList<IObject> Objects => _data.Objects;
    public IReadOnlyList<IGroup> Groups { get; }
    public IReadOnlyList<ISmoothingGroup> SmoothingGroups => _data.SmoothingGroups;
    public IReadOnlyList<VertexPosition> VertexPositions => _data.VertexPositions;
    public IReadOnlyList<VertexNormal> VertexNormals => _data.VertexNormals;
    public IReadOnlyList<VertexTextureCoord> VertexTextureCoords => _data.VertexTextureCoords;
    public IReadOnlyList<Face> Faces => _data.Faces;

    private readonly RawModelData _data;

    public WavefrontObjFileContents(RawModelData data)
    {
        _data = data;
    }
}