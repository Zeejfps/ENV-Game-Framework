using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Components.DataGrid;

/// <summary>What an editor cell is built with: the canvas to draw through, the input system to register
/// controllers on, the grid's <see cref="DataGridEditSession"/> for an editor's controller to drive the edit
/// lifecycle (commit / cancel / move), and the grid style for theming. Passed to
/// <see cref="DataGridColumn{TItem}.CreateEditor"/>.</summary>
public readonly record struct DataGridEditorContext(
    ICanvas Canvas, InputSystem Input, DataGridEditSession Session, DataGridStyle Style);

/// <summary>
/// One column of a <see cref="DataGridView{TItem}"/>: how wide it is, how it labels itself, and how it turns
/// an item into a cell. A column always has a <em>preview</em> cell (cheap, display-only; the kind recycled
/// into every visible row). A column is <em>editable</em> exactly when it also supplies an editor cell — the
/// rich widget that goes live in the editable-row pool. Read-only columns render their preview even inside an
/// editable row.
/// </summary>
public sealed record DataGridColumn<TItem>
{
    /// <summary>Stable identity for the column (focus/sort/persistence keying); not displayed.</summary>
    public required string Key { get; init; }

    public Prop<string> Header { get; init; }

    public ColumnWidth Width { get; init; } = ColumnWidth.Flex();

    /// <summary>Horizontal alignment of the cell content (e.g. <see cref="TextAlignment.End"/> for amounts).</summary>
    public TextAlignment Align { get; init; } = TextAlignment.Start;

    /// <summary>Builds the preview cell widget once per pooled row. When null, the grid uses a text cell
    /// driven by <see cref="Text"/>.</summary>
    public Func<ICanvas, View>? CreateCell { get; init; }

    /// <summary>Pushes an item's data into a preview cell built by <see cref="CreateCell"/>.</summary>
    public Action<View, TItem>? BindCell { get; init; }

    /// <summary>Convenience for the common text column: the cell text for an item. Used when
    /// <see cref="CreateCell"/> is null.</summary>
    public Func<TItem, string>? Text { get; init; }

    /// <summary>Builds the editor cell widget once per pooled editable row. Supplying it is what makes the
    /// column editable. The editor's own controller drives the edit lifecycle through the
    /// <see cref="DataGridEditSession"/> in the context.</summary>
    public Func<DataGridEditorContext, View>? CreateEditor { get; init; }

    /// <summary>Loads an item's current value into the editor when an edit begins (e.g. set text, select all).</summary>
    public Action<View, TItem>? BindEditor { get; init; }

    /// <summary>Reads the editor's value and applies it to the item/store when the edit commits.</summary>
    public Action<View, TItem>? CommitEditor { get; init; }

    /// <summary>Optional pre-commit check for an editable cell: returns false to reject the current value and
    /// keep the row in edit mode (e.g. an unparseable date or amount). Null means always valid. The grid checks
    /// every editable cell across the row before any cell commits, so a row commits all-or-nothing — except on a
    /// forced commit (focus lost / clicking away), which is best-effort and writes whatever parses.</summary>
    public Func<View, bool>? ValidateEditor { get; init; }

    /// <summary>Optional: render an editor's validity state (called with <c>invalid: true</c> when a non-forced
    /// commit is rejected by <see cref="ValidateEditor"/>, and with <c>false</c> to clear). The framework stays
    /// unopinionated about the cue — a consumer might tint the text, draw a border, etc. Null = no visual.</summary>
    public Action<View, bool>? MarkInvalid { get; init; }

    public bool Sortable { get; init; }

    /// <summary>True when this column can be edited — i.e. it supplies an editor cell.</summary>
    public bool IsEditable => CreateEditor != null;
}
