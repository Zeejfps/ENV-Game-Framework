using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>What a metadata grid's column headings are drawn from: each shows its filter text or
/// its name, then the funnel that filters it.</summary>
internal readonly struct MetadataGridHeading
{
    private const int FunnelSalt = 0x4D444746;
    private const int FilterTextSalt = 0x4D444754;

    private readonly MetadataGrid _grid;
    private readonly MetadataGridState _state;
    private readonly MetadataFilters _flags;
    private readonly Strings _t;
    private readonly AppIcons _icons;
    private readonly int _listId;

    public MetadataGridHeading(MetadataGrid grid, MetadataGridState state, MetadataFilters flags, Strings t, AppIcons icons, int listId)
    {
        _grid = grid;
        _state = state;
        _flags = flags;
        _t = t;
        _icons = icons;
        _listId = listId;
    }

    public static int FunnelId(int listId, int column) => HashCode.Combine(listId, FunnelSalt, column);

    public static void Render(Ui ui, int column, bool hovered, in MetadataGridHeading heading)
    {
        heading.RenderColumn(ui, column, hovered);
    }

    private void RenderColumn(Ui ui, int column, bool hovered)
    {
        var filter = _state.FilterOf(column, _flags);
        var textId = HashCode.Combine(_listId, FilterTextSalt, column);
        if (_state.EditingColumn == column)
            EditFilterText(ui, filter, textId);
        else if (filter.Text.Length > 0)
            FilterText(ui, filter, textId, column);
        else
            ColumnName(ui, _grid.Columns[column].Name, hovered);
        Funnel(ui, filter, column);
    }

    private void EditFilterText(Ui ui, MetadataColumnFilter filter, int textId)
    {
        var text = filter.Text;
        using (ui.Id(textId))
        {
            var edit = ui.InlineEdit("filter", ref text, _state.BeginEdit, Sizing.Grow());
            _state.BeginEdit = false;
            filter.Text = edit == InlineEditResult.Cancelled ? "" : text;
            if (edit != InlineEditResult.Editing)
                _state.StopEditingFilter();
        }
    }

    private void FilterText(Ui ui, MetadataColumnFilter filter, int textId, int column)
    {
        var theme = ui.Theme;
        var shown = ui.Interact(textId);
        if (shown.Hovered)
            ui.SetCursor(MouseCursor.Text);
        if (shown.Clicked)
            _state.StartEditingFilter(column);
        ui.Text(filter.Text, new Box { Width = Sizing.Grow() }, color: theme.AccentText, size: theme.FontSizeSmall, id: shown.Id,
            overflow: TextOverflow.Ellipsis);
    }

    private static void ColumnName(Ui ui, string name, bool hovered)
    {
        var theme = ui.Theme;
        var color = hovered ? theme.Text : theme.TextMuted;
        ui.Text(name, new Box { Width = Sizing.Grow() }, color: color, size: theme.FontSizeSmall, overflow: TextOverflow.Ellipsis);
    }

    private void Funnel(Ui ui, MetadataColumnFilter filter, int column)
    {
        var theme = ui.Theme;
        var funnel = ui.Clickable(FunnelId(_listId, column));
        if (funnel.Clicked)
            ClickFunnel(filter, column);
        var glyph = FunnelGlyph(_icons, filter);
        var color = FunnelColor(theme, filter, funnel.Hovered);
        using (ui.Box(funnel.Id, new Box { Width = theme.IconSize, Height = Sizing.Grow(), AlignX = Align.Center, AlignY = Align.Center }))
            ui.Glyph(glyph, color, theme.FontSizeSmall);
        if (!_state.IsFlagsFlyoutOpenFor(column))
            ui.Tooltip(funnel.Id, FunnelTooltip(ui, funnel.Id, _grid.Columns[column].Name));
    }

    private string FunnelTooltip(Ui ui, int funnelId, string name)
    {
        var t = _t;
        using (ui.Id(funnelId))
            return ui.Memo("tooltip", (name, t), static a => a.t.MetadataFilter(a.name));
    }

    private void ClickFunnel(MetadataColumnFilter filter, int column)
    {
        if (filter.Flags != null)
        {
            _state.ToggleFlagsFlyout(column);
            return;
        }
        if (filter.Text.Length > 0)
        {
            filter.Text = "";
            _state.StopEditingFilter();
            return;
        }
        _state.StartEditingFilter(column);
    }

    private static IconGlyph FunnelGlyph(AppIcons icons, MetadataColumnFilter filter)
    {
        if (filter.Flags != null)
            return icons.MetadataFilter;
        if (filter.Text.Length > 0)
            return icons.MetadataClearFilter;
        return icons.MetadataFilter;
    }

    private static uint FunnelColor(UiTheme theme, MetadataColumnFilter filter, bool hovered)
    {
        if (filter.IsActive)
            return theme.AccentText;
        if (hovered)
            return theme.Text;
        return theme.TextMuted;
    }
}
