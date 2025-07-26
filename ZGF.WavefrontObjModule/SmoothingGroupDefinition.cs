namespace ZGF.WavefrontObjModule;

internal sealed class SmoothingGroupDefinition
{
    public required int Id { get; init; }
    public required bool IsOff { get; init; }
    public Range FacesRange { get; set; }
}