using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Row in the local-changes lists. Same badge+path content as FileChangeRowView, but
/// clickable with reactive selection and hover backgrounds. Selection lives on the
/// view model (one <see cref="Selection"/> for both sides), not on the panel, so the
/// row's highlight binding just asks "is my (path, side) in the current selection?".
/// Modifier semantics (plain = single-select, Shift = range, Ctrl/Cmd = toggle) live
/// on the VM's <c>SelectRow</c> — the row forwards the modifiers and its identity.
/// </summary>
internal sealed class SelectableFileRowView : MultiChildView
{
    private const int RowVerticalPadding = 2;
    private const int RowHorizontalPadding = 4;

    public SelectableFileRowView(
        FileChange file,
        DiffSide side,
        IReadable<Selection> selection,
        Action<DiffTarget, InputModifiers> onClick,
        Action<DiffTarget>? onActivate = null)
    {
        var isHovered = new State<bool>(false);
        var path = file.Path;
        var target = new DiffTarget(path, side);

        var pathText = new TextView { Text = FileChangesPalette.FormatPath(file) };
        pathText.BindTextColor(() => selection.Value.Contains(path, side)
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
            selection.Value.Contains(path, side) ? DialogPalette.RowActive
            : isHovered.Value ? DialogPalette.RowHover
            : DialogPalette.RowTransparent);

        AddChildToSelf(background);

        this.UseController(_ => new SelectableRowController(
            mods => onClick(target, mods),
            h => isHovered.Value = h,
            onActivate != null ? () => onActivate(target) : null));
    }
}
