using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

public sealed class RepoBarContextMenuController : KeyboardMouseController
{
    private readonly Func<PointF, IReadOnlyList<RepoBarContextMenu.Item>> _buildItems;

    public RepoBarContextMenuController(Func<PointF, IReadOnlyList<RepoBarContextMenu.Item>> buildItems)
    {
        _buildItems = buildItems;
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Bubbling) return;
        if (e.Button != MouseButton.Right) return;
        if (e.State != InputState.Pressed) return;

        var ctx = Context;
        if (ctx == null) return;

        var anchor = e.Mouse.Point;
        var items = _buildItems(anchor);
        if (items.Count == 0) return;

        RepoBarContextMenu.Show(ctx, anchor, items);
        e.Consume();
    }
}
