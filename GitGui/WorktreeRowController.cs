using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Tests;
using ZGF.KeyboardModule;

namespace GitGui;

// Click → activate; right-click → context menu. Intentionally has no drag handling:
// worktrees follow their primary repo's placement, so they aren't user-orderable.
public sealed class WorktreeRowController : KeyboardMouseController
{
    private readonly Context _context;
    private readonly Repo _worktree;
    private readonly IRepoRegistry _registry;
    private readonly Action<bool> _onHoverChanged;
    private readonly Func<PointF, IReadOnlyList<RepoBarContextMenu.Item>> _buildMenuItems;

    public WorktreeRowController(
        Context context,
        Repo worktree,
        IRepoRegistry registry,
        Action<bool> onHoverChanged,
        Func<PointF, IReadOnlyList<RepoBarContextMenu.Item>> buildMenuItems)
    {
        _context = context;
        _worktree = worktree;
        _registry = registry;
        _onHoverChanged = onHoverChanged;
        _buildMenuItems = buildMenuItems;
    }

    public override void OnMouseEnter(ref MouseEnterEvent e) => _onHoverChanged(true);
    public override void OnMouseExit(ref MouseExitEvent e) => _onHoverChanged(false);

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Bubbling) return;

        if (e.Button == MouseButton.Right && e.State == InputState.Pressed)
        {
            var items = _buildMenuItems(e.Mouse.Point);
            if (items.Count > 0)
            {
                RepoBarContextMenu.Show(_context, e.Mouse.Point, items);
                e.Consume();
            }
            return;
        }

        if (e.Button != MouseButton.Left) return;
        if (e.State != InputState.Released) return;

        _registry.SetActive(_worktree.Id);
        e.Consume();
    }
}
