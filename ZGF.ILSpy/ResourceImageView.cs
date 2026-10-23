using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Resources;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>A picture resource's tab: what it is and an Export button above the picture, at its
/// own size on a framed surface, scrolling when larger than the view.</summary>
internal static class ResourceImageView
{
    private sealed class Uploaded
    {
        public ResourceImage? Image;
    }

    public static void Render(Ui<UiState> ui, Workspace w, DocumentTab tab, ResourceImage image)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var uploaded = ui.GetState<Uploaded>(ui.GetId(tab.ImageId));
        if (!ReferenceEquals(uploaded.Image, image) || !ui.HasImage(tab.ImageId))
        {
            ui.SetImage(tab.ImageId, image.Width, image.Height, image.Rgba);
            uploaded.Image = image;
        }

        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f, Background = theme.PaneBackground });
        using (ui.PaneToolbar(new Box { Gap = theme.Gap }))
        {
            ui.Text(t.ResourceImageInfo(image.Format, image.Width, image.Height), new Box { Width = Sizing.Grow() }, color: theme.TextMuted,
                overflow: TextOverflow.Ellipsis);
            if (tab.Node is { HasResource: true } node && ui.Button(t.ResourceExport, id: "export", icon: ExportIcon))
                PickExport(ui, w, node);
        }
        ui.Separator();
        using (ui.Scroll("image", ScrollAxes.Both, new Box { Height = Sizing.Grow(), Padding = theme.ShellPaddingX }))
        {
            using (ui.Box(new Box
            {
                Padding = theme.GapTight,
                Background = theme.Surface,
                BorderColor = theme.Border,
                BorderWidth = theme.BorderWidth,
            }))
                ui.Image(tab.ImageId, image.Width, image.Height);
        }
    }

    public static readonly UiIcon ExportIcon = new(LucideIcons.Upload, LucideIcons.Family);

    /// <summary>Asks where to export the resource <paramref name="node"/> stands for, then writes it there.</summary>
    public static void PickExport(Ui<UiState> ui, Workspace w, BrowseNode node)
    {
        var t = ui.State.Strings;
        ui.PickSaveFile(t.ResourceExportTitle, null, node.ResourceFileName, [new FileFilter(t.FileFilterAll, ["*.*"])],
            path => w.SaveResource(node, path));
    }
}
