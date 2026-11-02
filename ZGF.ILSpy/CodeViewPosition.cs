using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>Where a code view stood: the line at its top, its sideways scroll and its selection —
/// what going back to a place restores.</summary>
public readonly record struct CodeViewPosition(int TopLine, float ScrollX, TextPosition Anchor, TextPosition Caret);

/// <summary>Where a metadata grid stood: the row at its top, its sideways scroll, and the row selected and
/// the column of its current cell, each null when there is none.</summary>
public readonly record struct GridPosition(int TopRow, float ScrollX, int? Row, int? Column);

/// <summary>Where a tab's view stood: a code view's position or a grid's. A struct with a case for each, as
/// every tab keeps its own every frame; the default is a code view at the top.</summary>
public readonly record struct ViewPosition
{
    private enum Kind : byte
    {
        Code,
        Grid,
    }

    private readonly Kind _kind;
    private readonly CodeViewPosition _code;
    private readonly GridPosition _grid;

    private ViewPosition(Kind kind, CodeViewPosition code, GridPosition grid)
    {
        _kind = kind;
        _code = code;
        _grid = grid;
    }

    public static ViewPosition OfCode(CodeViewPosition position) => new(Kind.Code, position, default);

    public static ViewPosition OfGrid(GridPosition position) => new(Kind.Grid, default, position);

    /// <summary>The code view's position, or null when this is a grid's.</summary>
    public CodeViewPosition? Code => _kind == Kind.Code ? _code : null;

    /// <summary>The grid's position, or null when this is a code view's.</summary>
    public GridPosition? Grid => _kind == Kind.Grid ? _grid : null;

    /// <summary><paramref name="position"/>, taken while the node was shown in <paramref name="shownIn"/>, when it
    /// still holds once the node is shown in <paramref name="language"/>: a grid's in any language, a code view's
    /// only in the same; else null.</summary>
    public static ViewPosition? KeptIn(ViewPosition? position, DecompilationLanguage shownIn, DecompilationLanguage language)
    {
        if (position == null)
            return null;
        if (position.Value._kind == Kind.Grid)
            return position;
        if (shownIn != language)
            return null;
        return position;
    }
}
