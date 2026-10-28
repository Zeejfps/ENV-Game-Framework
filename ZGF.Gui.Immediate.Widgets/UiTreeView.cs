using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>What a <see cref="UiWidgets.TreeView{TNode}"/> call did. <see cref="Node"/> is the
/// selected node whenever a flag is set. <see cref="SelectionChanged"/> says the user picked it;
/// <see cref="SelectionMoved"/> says the selection moved up to it, or cleared, because the node selected
/// before is gone; <see cref="Toggled"/> says the user expanded or collapsed
/// <see cref="ToggledNode"/>.</summary>
public readonly record struct TreeViewResult<TNode>(
    bool SelectionChanged,
    bool Activated,
    bool ContextMenuRequested,
    PointF ContextMenuAt,
    TNode? Node)
{
    public bool SelectionMoved { get; init; }
    public bool Toggled { get; init; }
    public TNode? ToggledNode { get; init; }
}

public static partial class UiWidgets
{
    private const int DisclosureSalt = 0x44495343;

    /// <summary>
    /// A virtualized tree over <paramref name="roots"/> and the hierarchy <paramref name="model"/>
    /// describes. Click selects, double-click or Enter activates (a double-click also expands or
    /// collapses unless <paramref name="toggleOnActivate"/> is false), the disclosure arrow toggles,
    /// and a right-click or the Menu key asks for a context menu at the pointer or the row. While
    /// focused, Up/Down/Home/End/PageUp/PageDown move the selection and Left/Right collapse, expand,
    /// or step to the parent or first child. The selection is drawn in <see cref="UiTheme.Selection"/>
    /// while the tree holds focus and <see cref="UiTheme.SelectionInactive"/> otherwise; labels end
    /// in an ellipsis where the pane is too narrow, and a row's <see cref="TreeRow.Tooltip"/> shows
    /// while the pointer rests on it. The tree is a Tab stop.
    /// </summary>
    public static TreeViewResult<TNode> TreeView<TNode>(
        this Ui ui, string id, IReadOnlyList<TNode> roots, ITreeModel<TNode> model, TreeViewState<TNode> state, Box box = default,
        bool toggleOnActivate = true)
        where TNode : notnull
    {
        var theme = ui.Theme;
        var treeId = ui.GetId(id);
        var rowHeight = theme.RowHeight;
        state.Sync(roots, model);
        var selectionMoved = state.TakeSelectionMoved();

        var before = state.SelectedIndex;
        var selection = before;
        var activated = false;
        var contextMenu = false;
        var toggled = default(TNode);
        var hasToggled = false;
        var menuAt = default(PointF);

        var focused = ui.ListArea(treeId);
        var ring = focused && ui.FocusVisible;
        if (focused)
        {
            TreeKeys(ui, state, model, ui.MeasureListPage(treeId, rowHeight), ref selection, ref activated, ref contextMenu, ref toggled, ref hasToggled);
            if (contextMenu && selection >= 0 && ui.TryGetRect(RowId(treeId, state, state.Rows[selection].Node), out var row))
                menuAt = new PointF(row.X + row.Height, row.Bottom);
        }

        var toggle = -1;
        var requested = state.TakeScrollRequest();
        if (requested >= 0)
            ui.ScrollRowIntoView(treeId, requested);
        using (var list = ui.VirtualScroll(treeId, state.Rows.Count, rowHeight, box))
        {
            for (var i = list.First; i < list.End; i++)
            {
                var flat = state.Rows[i];
                var rowId = RowId(treeId, state, flat.Node);
                var row = ui.Interact(rowId);
                var hasChildren = model.HasChildren(flat.Node);
                var arrowId = HashCode.Combine(rowId, DisclosureSalt);
                var arrow = hasChildren ? ui.Interact(arrowId) : Interaction.Inert(arrowId);

                if (arrow.Pressed)
                {
                    toggle = i;
                    ui.Focus(treeId, takesText: false);
                }
                if (row.Pressed)
                {
                    selection = i;
                    ui.Focus(treeId, takesText: false);
                    if (row.DoubleClicked)
                    {
                        activated = true;
                        if (hasChildren && toggleOnActivate) toggle = i;
                    }
                }
                if (row.ClickedWith(PointerButton.Right))
                {
                    selection = i;
                    contextMenu = true;
                    menuAt = ui.Input.Pointer;
                    ui.Focus(treeId, takesText: false);
                }

                var content = model.Row(flat.Node);
                var expanded = hasChildren && state.IsExpanded(flat.Node);
                var selected = i == selection;
                var background = theme.PickRowBackground(selected, focused, row.Hovered);

                using (ui.Row(rowId, new Box
                {
                    Width = Sizing.Grow(),
                    Height = rowHeight,
                    Gap = theme.GapTight,
                    Padding = new Spacing(theme.TreePaddingX + flat.Depth * theme.TreeIndent, 0f, theme.TreePaddingX, 0f),
                    Background = background,
                }))
                {
                    if (selected && ring) ui.FocusRing(0f);
                    using (ui.Box(hasChildren ? arrowId : 0, new Box { Width = theme.DisclosureWidth, Height = Sizing.Grow() }))
                    {
                        if (hasChildren)
                            ui.PaintGlyph(expanded ? UiGlyph.DisclosureOpen : UiGlyph.DisclosureClosed, arrow.Hovered ? theme.Text : theme.TextMuted);
                    }
                    RowMark(ui, content, background);
                    ui.Text(content.Label, new Box { Width = Sizing.Fit() with { Priority = 1 } },
                        color: content.LabelColor != 0 ? content.LabelColor : selected ? theme.SelectionText : theme.Text, overflow: TextOverflow.Ellipsis);
                    if (content.Secondary != null)
                        ui.Text(content.Secondary, color: theme.TextMuted, overflow: TextOverflow.Ellipsis);
                    if (content.SecondaryBold != null)
                        ui.Text(content.SecondaryBold, new Box { Width = Sizing.Fit() with { Priority = 1 } }, color: theme.TextMuted, bold: true,
                            overflow: TextOverflow.Ellipsis);
                    if (content.BadgesSpinning)
                        BadgesSpinner(ui, background);
                    else if (content.Badges != null)
                        RowBadges(ui, content.Badges);
                }
                if (content.Tooltip != null)
                    ui.Tooltip(rowId, content.Tooltip);
            }
        }

        if (selection != before && selection >= 0)
            state.SelectAt(selection);
        if (toggle >= 0)
        {
            toggled = state.Rows[toggle].Node;
            hasToggled = true;
            if (state.IsExpanded(state.Rows[toggle].Node))
                state.Collapse(toggle);
            else
                state.Expand(toggle, model);
            ui.RequestUpdate();
        }

        var picked = selection != before;
        if (picked || selectionMoved)
            ui.ScrollRowIntoView(treeId, state.SelectedIndex);
        if (!picked && !selectionMoved && !activated && !contextMenu && !hasToggled)
            return default;
        return new TreeViewResult<TNode>(picked, activated && state.HasSelection, contextMenu && state.HasSelection, menuAt, state.Selected)
        {
            SelectionMoved = selectionMoved,
            Toggled = hasToggled,
            ToggledNode = hasToggled ? toggled : default,
        };
    }

    /// <summary>A row's icon, or while its node is at work the spinner in the icon's place, in the
    /// icon's color when it has one. Its dots fade toward the row's background, or the pane's when
    /// the row has none of its own.</summary>
    private static void RowMark(Ui ui, TreeRow content, uint background)
    {
        if (content.Spinning)
        {
            RowSpinner(ui, content.Icon, background);
            return;
        }
        if (content.Icon is { } icon)
            ui.Icon(icon);
    }

    /// <summary>A row's badges, each its glyph and text in its color, sized to fit so they keep their
    /// room while the secondary text gives up its own.</summary>
    private static void RowBadges(Ui ui, IReadOnlyList<TreeBadge> badges)
    {
        var theme = ui.Theme;
        var size = theme.FontSizeSmall;
        var fit = new Box { Width = Sizing.Fit() with { Priority = 1 } };
        foreach (var badge in badges)
        {
            using (ui.Row(new Box { Width = Sizing.Fit() with { Priority = 1 }, Height = Sizing.Grow(), Gap = 1f, AlignY = Align.Center }))
            {
                ui.Glyph(badge.Glyph, badge.Color, size, fit);
                ui.Text(badge.Text, fit, color: badge.Color, size: size);
            }
        }
    }

    /// <summary>The spinner in a row's badges' place, as small as their text, fading toward the row's
    /// background, or the pane's when the row has none of its own.</summary>
    private static void BadgesSpinner(Ui ui, uint background)
    {
        var theme = ui.Theme;
        var behind = background != 0 ? background : theme.PaneBackground;
        using (ui.Box(new Box { Width = Sizing.Fit() with { Priority = 1 }, Height = Sizing.Grow(), AlignY = Align.Center }))
            ui.Spinner(behind, theme.FontSizeSmall);
    }

    private static void RowSpinner(Ui ui, UiIcon? icon, uint background)
    {
        var theme = ui.Theme;
        var behind = background != 0 ? background : theme.PaneBackground;
        uint? color = null;
        if (icon != null && icon.Value.Color != 0)
            color = icon.Value.Color;
        ui.Spinner(behind, color: color);
    }

    private static int RowId<TNode>(int treeId, TreeViewState<TNode> state, TNode node) where TNode : notnull =>
        HashCode.Combine(treeId, state.Comparer.GetHashCode(node));

    private static void TreeKeys<TNode>(
        Ui ui, TreeViewState<TNode> state, ITreeModel<TNode> model, int page,
        ref int selection, ref bool activated, ref bool contextMenu, ref TNode? toggled, ref bool hasToggled)
        where TNode : notnull
    {
        foreach (var stroke in ui.Input.Keys)
        {
            var count = state.Rows.Count;
            if (count == 0) return;
            var current = selection;
            if (ApplyListKey(stroke, ref selection, count, page))
            {
                if (selection != current)
                    state.SelectAt(selection);
                continue;
            }
            if ((stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Super)) != 0) continue;
            switch (stroke.Key)
            {
                case KeyboardKey.LeftArrow when current >= 0:
                    var node = state.Rows[current].Node;
                    if (state.IsExpanded(node) && model.HasChildren(node))
                    {
                        state.Collapse(current);
                        toggled = node;
                        hasToggled = true;
                    }
                    else if (state.ParentIndex(current) is var parent and >= 0)
                        selection = parent;
                    break;
                case KeyboardKey.RightArrow when current >= 0:
                    var row = state.Rows[current];
                    if (!model.HasChildren(row.Node)) break;
                    if (!state.IsExpanded(row.Node))
                    {
                        state.Expand(current, model);
                        toggled = row.Node;
                        hasToggled = true;
                    }
                    else if (current + 1 < state.Rows.Count && state.Rows[current + 1].Depth > row.Depth)
                        selection = current + 1;
                    break;
                case KeyboardKey.Enter when current >= 0:
                    activated = true;
                    break;
                case KeyboardKey.Menu when current >= 0:
                case KeyboardKey.F10 when current >= 0 && (stroke.Modifiers & KeyModifiers.Shift) != 0:
                    contextMenu = true;
                    break;
            }
            if (selection != current && selection >= 0)
                state.SelectAt(selection);
        }
    }
}
