namespace ZGF.WavefrontObjModule;

internal sealed class NamedObject
{
    public string? Name { get; set; }
    public List<VertexPosition>? VertexPositions { get; set; }
    public List<VertexNormal>? VertexNormals { get; set; }
    public List<VertexTextureCoord>? VertexTextureCoords { get; set; }
    public List<Face>? Faces { get; set; }
}