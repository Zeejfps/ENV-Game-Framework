using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

public sealed class GroupHeaderController : KeyboardMouseController
{
    private readonly Group _group;
    private readonly IRepoRegistry _registry;
    private readonly Action<bool> _onHoverChanged;
    private readonly Func<PointF, IReadOnlyList<RepoBarContextMenu.Item>> _buildMenuItems;

    private IDragController? _dragController;

    public GroupHeaderController(
        Group group,
        IRepoRegistry registry,
        Action<bool> onHoverChanged,
        Func<PointF, IReadOnlyList<RepoBarContextMenu.Item>> buildMenuItems)
    {
        _group = group;
        _registry = registry;
        _onHoverChanged = onHoverChanged;
        _buildMenuItems = buildMenuItems;
    }

    protected override void OnAttachedToContext(View view, Context context)
    {
        _dragController = context.Get<IDragController>();
        _dragController?.RegisterGroupHeader(view, _group.Id);
    }

    protected override void OnDetachedFromContext(View view, Context context)
    {
        _dragController?.Unregister(view);
        _dragController = null;
    }

    public override void OnMouseEnter(ref MouseEnterEvent e) => _onHoverChanged(true);
    public override void OnMouseExit(ref MouseExitEvent e) => _onHoverChanged(false);

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Bubbling) return;
        if (e.State != InputState.Pressed) return;

        if (e.Button == MouseButton.Right)
        {
            var items = _buildMenuItems(e.Mouse.Point);
            if (items.Count > 0 && Context is not null)
            {
                RepoBarContextMenu.Show(Context, e.Mouse.Point, items);
                e.Consume();
            }
            return;
        }

        if (e.Button == MouseButton.Left)
        {
            if (_registry.RenamingGroupId.Value == _group.Id) return;
            _registry.ToggleGroupCollapsed(_group.Id);
            e.Consume();
        }
    }
}
