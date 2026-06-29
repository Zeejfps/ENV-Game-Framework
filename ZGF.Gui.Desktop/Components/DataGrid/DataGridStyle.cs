namespace ZGF.Gui.Desktop.Components.DataGrid;

/// <summary>The per-row visual state the grid pushes into a row when binding it. Selection, hover, and the
/// zebra stripe are derived by the grid (from its selection set and the list's hovered index); the row only
/// renders them. <see cref="Focused"/> is reserved for the editable phase. <see cref="Flash"/> is a transient
/// reveal highlight the owner sets via <see cref="DataGridView{TItem}.SetFlash"/>.</summary>
public readonly record struct DataGridRowState(
    bool Selected, bool Hovered, bool Stripe, bool Focused, bool NewRow = false, bool Flash = false);

/// <summary>
/// Colors and metrics for a <see cref="DataGridView{TItem}"/>. Defaults to a neutral dark theme; a consumer
/// overrides whatever it needs (the ledger supplies its own palette here).
/// </summary>
public sealed record DataGridStyle
{
    public float RowHeight { get; init; } = 28f;
    public float HeaderHeight { get; init; } = 30f;
    /// <summary>Width of the scrollbar gutter the header insets its content by, so its labels and dividers
    /// line up with the body cells (which the scrollbar pushes left).</summary>
    public float ScrollbarWidth { get; init; } = 12f;
    /// <summary>The smallest a flex column is allowed to shrink to when a neighbour is dragged wider.</summary>
    public float MinFlexWidth { get; init; } = 80f;
    public bool Striped { get; init; } = true;
    public bool ShowSelectionBar { get; init; } = true;

    public uint Surface { get; init; } = 0xFF1A1A1Du;
    public uint Stripe { get; init; } = 0xFF1E1E22u;
    public uint NewRow { get; init; } = 0xFF1C2420u;
    public uint RowHover { get; init; } = 0xFF23232Au;
    public uint SelectedRow { get; init; } = 0xFF22304Au;
    /// <summary>The transient reveal-highlight background for a flashed row (e.g. a just-inserted entry).</summary>
    public uint FlashRow { get; init; } = 0xFF2A3A2Eu;
    public uint SelectionBar { get; init; } = 0xFF3B82F6u;
    public uint Text { get; init; } = 0xFFEDEDEDu;
    public uint HeaderSurface { get; init; } = 0xFF202024u;
    public uint HeaderText { get; init; } = 0xFFAEB4BEu;
    public uint Border { get; init; } = 0xFF34343Cu;

    public static readonly DataGridStyle Default = new();
}
