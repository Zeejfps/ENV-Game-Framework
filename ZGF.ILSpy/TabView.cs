namespace ZGF.ILSpy;

/// <summary>How one open tab is shown: its code view, its grid view, its picture, and the image the
/// picture is drawn as. Made when the tab opens, and dropped, with its image, when it closes.</summary>
public sealed class TabView
{
    public TabView(DocumentTab tab)
    {
        ImageId = $"{tab.Panel}-image";
    }

    public CodeViewState Code { get; } = new();

    public MetadataGridState Grid { get; } = new();

    public PictureView Picture { get; } = new();

    /// <summary>The image the tab's picture is drawn as.</summary>
    public string ImageId { get; }

    /// <summary>Starts the grid view over on a grid <paramref name="tab"/> shows that it did not, and the
    /// picture view on a new picture.</summary>
    public void Follow(DocumentTab tab)
    {
        Picture.Follow(tab.Image);
        var grid = tab.Grid;
        if (grid == null)
            return;
        Grid.Show(grid);
    }

    /// <summary>Where the view of what <paramref name="tab"/> shows stands: its grid's or its code's.</summary>
    public CodeViewPosition PositionOf(DocumentTab tab)
    {
        if (tab.Grid != null)
            return Grid.Position;
        return Code.Position;
    }
}
