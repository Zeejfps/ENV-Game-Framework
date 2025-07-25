namespace ZGF.WavefrontObjModule;

public sealed class WavefrontObjFileContents : IWavefrontObjFileContents
{
    public IEnumerable<INamedObject> NamedObjects { get; }
    
    public WavefrontObjFileContents()
    {
               
    }
}

public sealed class NamedObject
{
    public string Name { get; set; }
}