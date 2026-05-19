using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Row in the local-changes lists. Same badge+path content as FileChangeRowView, but
/// clickable with reactive selection and hover backgrounds. The panel above handles
/// the modifier semantics (plain = single-select, Shift = range, Ctrl/Cmd = toggle);
/// this row just forwards the click + modifier state and renders the resulting state.
/// </summary>
internal sealed class SelectableFileRowView : MultiChildView
{
    private const int RowVerticalPadding = 2;
    private const int RowHorizontalPadding = 4;

    public SelectableFileRowView(
        FileChange file,
        IReadable<HashSet<string>> selection,
        Action<string, InputModifiers> onClick,
        Action<string>? onActivate = null)
    {
        var isHovered = new State<bool>(false);
        var path = file.Path;

        var pathText = new TextView { Text = FileChangesPalette.FormatPath(file) };
        pathText.BindTextColor(() => selection.Value.Contains(path)
            ? DialogPalette.RowTextActive
            : DialogPalette.RowText);

        var content = new FlexRowView
        {
            Gap = 8f,
            CrossAxisAlignment = CrossAxisAlignment.Start,
            Children =
            {
                FileChangesUI.CreateStatusBadge(file),
                new FlexItem { Grow = 1, Child = pathText },
            },
        };

        var background = new RectView
        {
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle
            {
                Left = RowHorizontalPadding,
                Right = RowHorizontalPadding,
                Top = RowVerticalPadding,
                Bottom = RowVerticalPadding,
            },
            Children = { content },
        };
        background.BindBackgroundColor(() =>
            selection.Value.Contains(path) ? DialogPalette.RowActive
            : isHovered.Value ? DialogPalette.RowHover
            : DialogPalette.RowTransparent);

        AddChildToSelf(background);

        this.UseController(_ => new SelectableRowController(
            mods => onClick(path, mods),
            h => isHovered.Value = h,
            onActivate != null ? () => onActivate(path) : null));
    }
}