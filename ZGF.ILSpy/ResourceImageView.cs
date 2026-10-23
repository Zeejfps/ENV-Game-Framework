using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Resources;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>A picture resource's tab: what it is, zoom buttons, a switch to its markup when it is
/// stored as such, and an Export button above the picture, zoomed on a framed surface, scrolling
/// when larger than the view.</summary>
internal static class ResourceImageView
{
    /// <summary>The picture last given to the canvas, and the zoom it was drawn at.</summary>
    private sealed class Uploaded
    {
        public ResourceImage? Image;
        public float Scale;
    }

    public static void Render(Ui<UiState> ui, Workspace w, DocumentTab tab, ResourceImage image)
    {
        var theme = ui.Theme;
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f, Background = theme.PaneBackground });
        Toolbar(ui, w, tab, image);
        ui.Separator();
        if (tab.ShowsSource)
        {
            DocumentsView.RenderCode(ui, w, tab);
            return;
        }
        Picture(ui, tab, image);
    }

    private static void Toolbar(Ui<UiState> ui, Workspace w, DocumentTab tab, ResourceImage image)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.PaneToolbar(new Box { Gap = theme.Gap }))
        {
            ui.Text(t.ResourceImageInfo(image.Format, image.Width, image.Height), new Box { Width = Sizing.Grow() }, color: theme.TextMuted,
                overflow: TextOverflow.Ellipsis);
            if (!tab.ShowsSource)
                ZoomButtons(ui, tab.Zoom);
            if (tab.HasSource)
                SourceButton(ui, tab);
            if (tab.Node is { HasResource: true } node && ui.Button(t.ResourceExport, id: "export", icon: ExportIcon))
                PickExport(ui, w, node);
        }
    }

    private static void ZoomButtons(Ui<UiState> ui, ImageZoom zoom)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        if (ui.IconButton("zoom-out", LucideIcons.ZoomOut, LucideIcons.Family, t.ResourceZoomOut, zoom.CanZoomOut))
            zoom.ZoomOut();
        ui.Text(t.ResourceZoomPercent(zoom.Percent), color: theme.TextMuted);
        if (ui.IconButton("zoom-in", LucideIcons.ZoomIn, LucideIcons.Family, t.ResourceZoomIn, zoom.CanZoomIn))
            zoom.ZoomIn();
        if (ui.IconButton("zoom-reset", LucideIcons.RotateCcw, LucideIcons.Family, t.ResourceZoomReset, !zoom.IsOwnSize))
            zoom.Reset();
        ui.Separator(theme.IconSize);
    }

    /// <summary>Switches to the markup while the picture shows, and back.</summary>
    private static void SourceButton(Ui<UiState> ui, DocumentTab tab)
    {
        var t = ui.State.Strings;
        var label = tab.ShowsSource ? t.ResourceShowPicture : t.ResourceShowSource;
        var glyph = tab.ShowsSource ? LucideIcons.FileImage : LucideIcons.FileText;
        if (ui.Button(label, id: "source", icon: new UiIcon(glyph, LucideIcons.Family)))
            tab.ToggleSource();
    }

    private static void Picture(Ui<UiState> ui, DocumentTab tab, ResourceImage image)
    {
        var theme = ui.Theme;
        var scale = tab.Zoom.Scale;
        Upload(ui, tab, image, scale);
        using (ui.Scroll("image", ScrollAxes.Both, new Box { Height = Sizing.Grow(), Padding = theme.ShellPaddingX }))
        {
            using (ui.Box(new Box
            {
                Padding = theme.GapTight,
                Background = theme.Surface,
                BorderColor = theme.Border,
                BorderWidth = theme.BorderWidth,
            }))
                ui.Image(tab.ImageId, image.Width * scale, image.Height * scale);
        }
    }

    /// <summary>Gives the canvas the picture when it changed, and a vector picture again, drawn
    /// sharp, when the zoom changed; a raster one is stretched instead.</summary>
    private static void Upload(Ui<UiState> ui, DocumentTab tab, ResourceImage image, float scale)
    {
        var uploaded = ui.GetState<Uploaded>(ui.GetId(tab.ImageId));
        if (!NeedsUpload(ui, tab, uploaded, image, scale))
            return;
        var drawn = image.DrawnAt(scale);
        ui.SetImage(tab.ImageId, drawn.Width, drawn.Height, drawn.Rgba);
        uploaded.Image = image;
        uploaded.Scale = scale;
    }

    private static bool NeedsUpload(Ui<UiState> ui, DocumentTab tab, Uploaded uploaded, ResourceImage image, float scale)
    {
        if (!ReferenceEquals(uploaded.Image, image))
            return true;
        if (!ui.HasImage(tab.ImageId))
            return true;
        return image.IsVector && uploaded.Scale != scale;
    }

    public static readonly UiIcon ExportIcon = new(LucideIcons.Upload, LucideIcons.Family);

    /// <summary>Asks where to export the resource <paramref name="node"/> stands for, then writes it there.</summary>
    public static void PickExport(Ui<UiState> ui, Workspace w, BrowseNode node)
    {
        var t = ui.State.Strings;
        var fileName = node.ResourceFileName;
        ui.PickSaveFile(t.ResourceExportTitle, null, fileName, ExportFilters(t, fileName), path => w.SaveResource(node, path));
    }

    private const string XamlExtension = ".xaml";

    /// <summary>A XAML filter first for a resource saved as XAML, then all files.</summary>
    private static FileFilter[] ExportFilters(Strings t, string fileName)
    {
        var all = new FileFilter(t.FileFilterAll, ["*.*"]);
        if (!IsXamlName(fileName))
            return [all];
        return [new FileFilter(t.FileFilterXaml, ["*" + XamlExtension]), all];
    }

    private static bool IsXamlName(string fileName) => Path.GetExtension(fileName).Equals(XamlExtension, StringComparison.OrdinalIgnoreCase);
}
