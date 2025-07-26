namespace ZGF.WavefrontObjModule;

internal sealed class SmoothingGroup : ISmoothingGroup
{
    public required int Id { get; set; }
    public required ReadOnlyMemory<Face> Faces { get; set; }
}