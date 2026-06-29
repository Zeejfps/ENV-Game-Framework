namespace ZGF.Gui.Desktop.Components.DataGrid;

/// <summary>
/// The edit-lifecycle callbacks the grid hands to each editor cell (via <see cref="DataGridEditorContext"/>),
/// so an editor's own keyboard controller can drive the session without knowing the grid: commit or cancel
/// the current cell, or move to an adjacent cell/row. The grid wires these to its focus model; an editor just
/// calls them. All default to no-ops so a partial editor is still safe.
/// </summary>
public sealed class DataGridEditSession
{
    /// <summary>Commit the current cell and end editing (e.g. on blur).</summary>
    public Action Commit { get; init; } = static () => { };

    /// <summary>Discard the current edit and end editing (Escape).</summary>
    public Action Cancel { get; init; } = static () => { };

    /// <summary>Commit and move to the next editable cell in the row (Tab).</summary>
    public Action MoveNext { get; init; } = static () => { };

    /// <summary>Commit and move to the previous editable cell in the row (Shift+Tab).</summary>
    public Action MovePrev { get; init; } = static () => { };

    /// <summary>Commit and move to the same cell in the row below (Enter / Down).</summary>
    public Action MoveDown { get; init; } = static () => { };

    /// <summary>Commit and move to the same cell in the row above (Up).</summary>
    public Action MoveUp { get; init; } = static () => { };
}
