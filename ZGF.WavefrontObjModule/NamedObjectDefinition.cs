namespace ZGF.WavefrontObjModule;

internal sealed class NamedObjectDefinition
{
    public required string Name { get; init; }
    public Range VertexPositionsRange { get; set; }
    public Range VertexNormalsRange { get; set; }
    public Range VertexTextureCoordsRange { get; set; }
    public Range FacesRange { get; set; }
}