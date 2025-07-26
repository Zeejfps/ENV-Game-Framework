namespace ZGF.WavefrontObjModule;

internal sealed class RawModelData
{
    public required Object[] Objects { get; init; }
    public required SmoothingGroup[] SmoothingGroups { get; set; }
    public required VertexPosition[] VertexPositions { get; init; }
    public required VertexNormal[] VertexNormals { get; init; }
    public required VertexTextureCoord[] VertexTextureCoords { get; init; }
    public required Face[] Faces { get; init; }
}