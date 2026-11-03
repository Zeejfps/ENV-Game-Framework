using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy;

/// <summary>How one open tab is shown: the panel it is docked as, the path its omnibar shows, its code view
/// and the note it shows while there is no code, its grid view, its picture, the image the picture is drawn as,
/// its diagram, and the rows of its comparison. Made when the tab opens, and dropped, with its image, when it closes.</summary>
public sealed class TabView
{
    public TabView(string panel)
    {
        Panel = panel;
        ImageId = $"{panel}-image";
    }

    /// <summary>The name of the tab's panel in the layout.</summary>
    public string Panel { get; }

    /// <summary>The path to the node the tab shows, for its omnibar.</summary>
    public Breadcrumb Breadcrumb { get; } = new();

    public CodeViewState Code { get; } = new();

    public TabPlaceholder Placeholder { get; } = new();

    public ShownReport Report { get; } = new();

    private MetadataGridState? _grid;

    public PictureView Picture { get; } = new();

    public DiagramViewState Diagram { get; } = new();

    public ComparisonRows Comparison { get; } = new();

    /// <summary>The image the tab's picture is drawn as.</summary>
    public string ImageId { get; }

    /// <summary>Starts the picture view over on a new picture.</summary>
    public void Follow(DocumentTab tab)
    {
        Picture.Follow(tab.Image);
    }

    /// <summary>The view of <paramref name="grid"/>: the one kept for it, or a new one when the tab
    /// showed another grid, or none, before.</summary>
    public MetadataGridState GridOf(MetadataGrid grid)
    {
        var shown = _grid;
        if (shown != null && ReferenceEquals(shown.Grid, grid))
            return shown;
        shown = new MetadataGridState(grid);
        _grid = shown;
        return shown;
    }

    /// <summary>Where the view of what <paramref name="tab"/> shows stands: its grid's or its code's.</summary>
    public ViewPosition PositionOf(DocumentTab tab)
    {
        var grid = tab.Grid;
        if (grid != null)
            return ViewPosition.OfGrid(GridOf(grid).Position);
        return ViewPosition.OfCode(Code.Position);
    }
}
