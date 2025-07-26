namespace ZGF.WavefrontObjModule;

public interface ISmoothingGroup
{
    int Id { get; }
    ReadOnlyMemory<Face> Faces { get; }
}