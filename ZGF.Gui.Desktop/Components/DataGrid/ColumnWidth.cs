namespace ZGF.Gui.Desktop.Components.DataGrid;

public enum ColumnWidthKind
{
    /// <summary>A constant pixel width.</summary>
    Fixed,
    /// <summary>Fills the leftover space, split between flex columns by weight.</summary>
    Flex,
    /// <summary>A user-resizable pixel width with a default, a floor, and a ceiling.</summary>
    Resizable,
}

/// <summary>
/// How a <see cref="DataGridColumn{TItem}"/> claims horizontal space. <see cref="Fixed"/> takes a constant
/// width; <see cref="Resizable"/> takes a draggable width clamped to <c>[min, max]</c>; <see cref="Flex"/>
/// absorbs whatever the fixed/resizable columns leave, divided between flex columns by weight.
/// </summary>
public readonly struct ColumnWidth
{
    private ColumnWidth(ColumnWidthKind kind, float value, float min, float max)
    {
        Kind = kind;
        Value = value;
        Min = min;
        Max = max;
    }

    public ColumnWidthKind Kind { get; }

    /// <summary>Pixels for <see cref="ColumnWidthKind.Fixed"/>, the default pixels for
    /// <see cref="ColumnWidthKind.Resizable"/>, or the weight for <see cref="ColumnWidthKind.Flex"/>.</summary>
    public float Value { get; }

    public float Min { get; }
    public float Max { get; }

    public static ColumnWidth Fixed(float pixels) =>
        new(ColumnWidthKind.Fixed, pixels, pixels, pixels);

    public static ColumnWidth Flex(float weight = 1f) =>
        new(ColumnWidthKind.Flex, weight <= 0f ? 1f : weight, 0f, float.PositiveInfinity);

    public static ColumnWidth Resizable(float defaultPixels, float min, float max) =>
        new(ColumnWidthKind.Resizable, defaultPixels, min, max);
}
