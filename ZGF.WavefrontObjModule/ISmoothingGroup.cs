namespace ZGF.WavefrontObjModule;

public interface ISmoothingGroup
{
    int Id { get; }
    bool IsOff { get; }
    ReadOnlySpan<Face> Faces { get; }
}