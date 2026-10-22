using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy;

/// <summary>What hovering a metadata grid's cell explains: text, or a flags value broken down into its
/// groups.</summary>
internal static class MetadataTooltipView
{
    public static void Render(Ui ui, MetadataTooltip content)
    {
        var theme = ui.Theme;
        if (content.Text != null)
        {
            ui.Text(content.Text, color: theme.Text, size: theme.FontSizeSmall, wrap: true);
            return;
        }
        using (ui.Column(new Box { Gap = theme.GapTight }))
        {
            foreach (var group in content.Groups)
                Group(ui, group);
        }
    }

    private static void Group(Ui ui, MetadataFlagGroup group)
    {
        if (group.IsChoice)
            Choice(ui, group);
        else
            Flags(ui, group);
    }

    private static void Choice(Ui ui, MetadataFlagGroup group)
    {
        var theme = ui.Theme;
        using (ui.Row(new Box { Gap = theme.GapTight }))
        {
            Header(ui, group);
            if (group.Flags.Count > 0)
                ui.Text(group.Flags[0].Name, color: theme.Text, size: theme.FontSizeSmall);
        }
    }

    private static void Flags(Ui ui, MetadataFlagGroup group)
    {
        using (ui.Column(new Box { Gap = 0f }))
        {
            Header(ui, group);
            foreach (var flag in group.Flags)
                Flag(ui, flag);
        }
    }

    private static void Header(Ui ui, MetadataFlagGroup group)
    {
        if (string.IsNullOrEmpty(group.Header))
            return;
        var theme = ui.Theme;
        ui.Text(group.Header, color: theme.Text, size: theme.FontSizeSmall, bold: true);
    }

    private static void Flag(Ui ui, MetadataFlag flag)
    {
        var theme = ui.Theme;
        var box = flag.IsSet ? LucideIcons.CheckSquare : LucideIcons.Square;
        var boxColor = flag.IsSet ? theme.AccentText : theme.TextMuted;
        var nameColor = flag.IsSet ? theme.Text : theme.TextMuted;
        using (ui.Row(new Box { Gap = theme.GapTight, AlignY = Align.Center }))
        {
            ui.Text(box, color: boxColor, size: theme.FontSizeSmall, family: LucideIcons.Family);
            ui.Text(flag.Name, color: nameColor, size: theme.FontSizeSmall);
        }
    }
}
