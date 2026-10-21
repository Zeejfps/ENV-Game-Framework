using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>What a <see cref="UiWidgets.TreeView{TNode}"/> call did. <see cref="Node"/> is the
/// selected node whenever a flag is set; <see cref="Toggled"/> says the user expanded or collapsed
/// <see cref="ToggledNode"/>.</summary>
public readonly record struct TreeViewResult<TNode>(
    bool SelectionChanged,
    bool Activated,
    bool ContextMenuRequested,
    PointF ContextMenuAt,
    TNode? Node)
{
    public bool Toggled { get; init; }
    public TNode? ToggledNode { get; init; }
}

public static partial class UiWidgets
{
    private const int DisclosureSalt = 0x44495343;
    private const float BadgePaddingX = 2f;

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

        var area = ui.Interact(treeId);
        if (area.Pressed || area.ClickedWith(PointerButton.Right))
            ui.Focus(treeId, takesText: false);
        var focused = ui.TabStop(treeId);
        var ring = focused && ui.FocusVisible;
        if (focused)
        {
            var page = ui.TryGetRect(treeId, out var view) ? Math.Max(1, (int)(view.Height / rowHeight) - 1) : 10;
            TreeKeys(ui, state, model, page, ref selection, ref activated, ref contextMenu, ref toggled, ref hasToggled);
            if (contextMenu && selection >= 0 && ui.TryGetRect(RowId(treeId, state, state.Rows[selection].Node), out var row))
                menuAt = new PointF(row.X + row.Height, row.Bottom);
        }

        var toggle = -1;
        using (var list = ui.VirtualScroll(treeId, state.Rows.Count, rowHeight, box with { Gap = 0f }))
        {
            var scrollTo = state.TakeScrollIndex();
            if (scrollTo >= 0 && list.ScrollIntoView(scrollTo))
                state.ScrollIndex = -1;

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
                var background = selected
                    ? focused ? theme.Selection : theme.SelectionInactive
                    : row.Hovered ? theme.RowHover : 0u;

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
                            ui.Paint((expanded, arrow.Hovered ? theme.Text : theme.TextMuted, theme.IconStrokeWidth),
                                static (p, s) => DrawDisclosure(p, s.expanded, s.Item2, s.IconStrokeWidth));
                    }
                    if (content.Icon != null)
                    {
                        using (ui.Box(new Box { Width = Sizing.Fit(theme.IconSize), Height = Sizing.Grow(), AlignX = Align.Center, AlignY = Align.Center }))
                        using (ui.Box())
                        {
                            ui.Text(content.Icon, content.IconBackground != 0
                                ? new Box
                                {
                                    Width = Sizing.Fit(theme.IconSize),
                                    Height = theme.IconSize,
                                    Padding = new Spacing(BadgePaddingX, 0f),
                                    Background = content.IconBackground,
                                    Radius = theme.RadiusSmall,
                                }
                                : default,
                                color: content.IconColor != 0 ? content.IconColor : theme.TextMuted, align: Align.Center, family: content.IconFamily);
                            if (content.IconOverlay != null)
                            {
                                var overlay = theme.IconOverlaySize;
                                using (ui.Float(1f, 1f, -overlay * 0.75f, -overlay * 0.75f, new Box { Width = overlay, Height = overlay }, z: 1))
                                    ui.Text(content.IconOverlay, new Box { Width = overlay, Height = overlay },
                                        color: content.IconOverlayColor != 0 ? content.IconOverlayColor : theme.TextMuted, size: overlay, align: Align.Center,
                                        family: content.IconOverlayFamily ?? content.IconFamily);
                            }
                        }
                    }
                    ui.Text(content.Label, new Box { Width = Sizing.Fit() with { Priority = 1 } },
                        color: content.LabelColor != 0 ? content.LabelColor : selected ? theme.SelectionText : theme.Text, overflow: TextOverflow.Ellipsis);
                    if (content.Secondary != null)
                        ui.Text(content.Secondary, color: theme.TextMuted, overflow: TextOverflow.Ellipsis);
                }
                if (content.Tooltip != null)
                    ui.Tooltip(rowId, content.Tooltip);
            }
        }

        if (selection != before && selection >= 0)
            state.SelectAt(selection);
        var moved = false;
        if (toggle >= 0)
        {
            toggled = state.Rows[toggle].Node;
            hasToggled = true;
            if (state.IsExpanded(state.Rows[toggle].Node))
                moved = state.Collapse(toggle);
            else
                state.Expand(toggle, model);
            ui.RequestFrame();
        }

        var changed = selection != before || moved || selectionMoved;
        if (!changed && !activated && !contextMenu && !hasToggled)
            return default;
        return new TreeViewResult<TNode>(changed, activated && state.HasSelection, contextMenu && state.HasSelection, menuAt, state.Selected)
        {
            Toggled = hasToggled,
            ToggledNode = hasToggled ? toggled : default,
        };
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
            if ((stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Super)) != 0) continue;
            var count = state.Rows.Count;
            if (count == 0) return;
            var current = selection;
            switch (stroke.Key)
            {
                case KeyboardKey.UpArrow:
                    selection = current < 0 ? 0 : Math.Max(0, current - 1);
                    break;
                case KeyboardKey.DownArrow:
                    selection = current < 0 ? 0 : Math.Min(count - 1, current + 1);
                    break;
                case KeyboardKey.PageUp:
                    selection = Math.Max(0, current - page);
                    break;
                case KeyboardKey.PageDown:
                    selection = Math.Min(count - 1, Math.Max(0, current) + page);
                    break;
                case KeyboardKey.Home:
                    selection = 0;
                    break;
                case KeyboardKey.End:
                    selection = count - 1;
                    break;
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

    private static void DrawDisclosure(UiPainter p, bool expanded, uint color, float stroke)
    {
        var r = p.Rect;
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        const float h = 3.5f;
        if (expanded)
        {
            p.Line(new PointF(cx - h, cy - h / 2), new PointF(cx, cy + h / 2), stroke, color);
            p.Line(new PointF(cx, cy + h / 2), new PointF(cx + h, cy - h / 2), stroke, color);
        }
        else
        {
            p.Line(new PointF(cx - h / 2, cy - h), new PointF(cx + h / 2, cy), stroke, color);
            p.Line(new PointF(cx + h / 2, cy), new PointF(cx - h / 2, cy + h), stroke, color);
        }
    }
}
