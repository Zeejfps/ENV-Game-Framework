namespace ZGF.WavefrontObjModule;

public interface IWavefrontObjFileContents
{
    IEnumerable<INamedObject> NamedObjects { get; }
}