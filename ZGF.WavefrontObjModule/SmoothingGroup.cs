namespace ZGF.WavefrontObjModule;

internal sealed class SmoothingGroup : ISmoothingGroup
{
    public required int Id { get; init; }
    public required ReadOnlyMemory<Face> Faces { get; init; }
}