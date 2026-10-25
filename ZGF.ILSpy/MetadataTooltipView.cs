using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy;

/// <summary>What hovering a metadata grid's cell explains: text, or a flags value broken down into its
/// groups.</summary>
internal static class MetadataTooltipView
{
    public static void Render(Ui<UiState> ui, MetadataTooltip content)
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

    private static void Group(Ui<UiState> ui, MetadataFlagGroup group)
    {
        if (group.IsChoice)
            Choice(ui, group);
        else
            Flags(ui, group);
    }

    private static void Choice(Ui<UiState> ui, MetadataFlagGroup group)
    {
        var theme = ui.Theme;
        using (ui.Row(new Box { Gap = theme.GapTight }))
        {
            Header(ui, group);
            if (group.Flags.Count > 0)
                ui.Text(group.Flags[0].Name, color: theme.Text, size: theme.FontSizeSmall);
        }
    }

    private static void Flags(Ui<UiState> ui, MetadataFlagGroup group)
    {
        using (ui.Column(new Box { Gap = 0f }))
        {
            Header(ui, group);
            foreach (var flag in group.Flags)
                Flag(ui, flag);
        }
    }

    private static void Header(Ui<UiState> ui, MetadataFlagGroup group)
    {
        if (string.IsNullOrEmpty(group.Header))
            return;
        var theme = ui.Theme;
        ui.Text(group.Header, color: theme.Text, size: theme.FontSizeSmall, bold: true);
    }

    private static void Flag(Ui<UiState> ui, MetadataFlag flag)
    {
        var theme = ui.Theme;
        var icons = ui.State.Icons;
        var box = flag.IsSet ? icons.MetadataFlagSet : icons.MetadataFlagUnset;
        var boxColor = flag.IsSet ? theme.AccentText : theme.TextMuted;
        var nameColor = flag.IsSet ? theme.Text : theme.TextMuted;
        using (ui.Row(new Box { Gap = theme.GapTight, AlignY = Align.Center }))
        {
            ui.Glyph(box, boxColor, theme.FontSizeSmall);
            ui.Text(flag.Name, color: nameColor, size: theme.FontSizeSmall);
        }
    }
}
