namespace ZGF.WavefrontObjModule;

public interface IGroup
{
    public string Name { get; }
    ReadOnlySpan<Face> Faces { get; }
}