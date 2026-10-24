namespace ZGF.ILSpy;

/// <summary>Where a tab's view stands once what it shows arrives: back at <see cref="Position"/>, else on
/// the line <see cref="Target"/> marks or the grid's <see cref="Row"/>, else on the node's definition.</summary>
public readonly record struct Arrival(
    CodeViewPosition? Position = null,
    Bookmark? Target = null,
    int Row = MetadataGridState.None);
