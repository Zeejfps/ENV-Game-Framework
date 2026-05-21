using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Tests;
using ZGF.KeyboardModule;

namespace GitGui;

// Click → activate the submodule's repo (so its branches/commits populate the side
// panels); right-click → context menu. Intentionally no drag handling — submodules
// follow their parent and aren't user-orderable. Clicks on a missing submodule (not
// initialized yet) do nothing because there's no working tree to activate.
public sealed class SubmoduleRowController : KeyboardMouseController
{
    private readonly Context _context;
    private readonly Repo _submodule;
    private readonly IRepoRegistry _registry;
    private readonly Action<bool> _onHoverChanged;
    private readonly Func<PointF, IReadOnlyList<RepoBarContextMenu.Item>> _buildMenuItems;

    public SubmoduleRowController(
        Context context,
        Repo submodule,
        IRepoRegistry registry,
        Action<bool> onHoverChanged,
        Func<PointF, IReadOnlyList<RepoBarContextMenu.Item>> buildMenuItems)
    {
        _context = context;
        _submodule = submodule;
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

        // A submodule that's been added but not initialized has no .git directory of its
        // own and would render an empty BranchesView/HistoryView. Better to do nothing —
        // the user can right-click → Update… to initialize it.
        if (_submodule.IsMissing) return;

        _registry.SetActive(_submodule.Id);
        e.Consume();
    }
}
