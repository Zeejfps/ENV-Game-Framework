namespace ZGF.WavefrontObjModule;

internal sealed class WavefrontObjFileContents : IWavefrontObjFileContents
{
    public IReadOnlyList<string> Comments => _data.Comments;
    public IReadOnlySet<string> MtlFiles => _data.MtlFiles;
    public IReadOnlyList<IObject> Objects => _data.Objects;
    public IReadOnlyList<IGroup> Groups { get; }
    public IReadOnlyList<ISmoothingGroup> SmoothingGroups => _data.SmoothingGroups;
    public ReadOnlyMemory<VertexPosition> VertexPositions => _data.VertexPositions;
    public ReadOnlyMemory<VertexNormal> VertexNormals => _data.VertexNormals;
    public ReadOnlyMemory<VertexTextureCoord> VertexTextureCoords => _data.VertexTextureCoords;
    public ReadOnlyMemory<Face> Faces => _data.Faces;

    private readonly RawModelData _data;

    public WavefrontObjFileContents(RawModelData data)
    {
        _data = data;
    }
}