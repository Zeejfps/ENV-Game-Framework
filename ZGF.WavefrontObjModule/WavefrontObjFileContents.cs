namespace ZGF.WavefrontObjModule;

public sealed class WavefrontObjFileContents : IWavefrontObjFileContents
{
    public IEnumerable<INamedObject> NamedObjects { get; }
    public IEnumerable<IGroup> Groups { get; }
    public IEnumerable<ISmoothingGroup> SmoothingGroups { get; }
    public IEnumerable<VertexPosition> AllVertexPositions { get; }
    public IEnumerable<VertexNormal> AllVertexNormals { get; }
    public IEnumerable<VertexTextureCoord> AllVertexTextureCoords { get; }

    public WavefrontObjFileContents()
    {
               
    }
}