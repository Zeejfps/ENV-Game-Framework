using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

public sealed class DropTarget
{
    public required Guid GroupId { get; init; }
    public required int InsertIndex { get; init; }
    public required RectF IndicatorBounds { get; init; }
}

public sealed class DragSession
{
    public required Repo Source { get; init; }
    public PointF MousePosition { get; set; }
}

public interface IDragController
{
    State<DragSession?> Session { get; }
    State<DropTarget?> Target { get; }
    void StartDrag(Repo source, PointF mouse);
    void UpdateDrag(PointF mouse);
    void CompleteDrag();
    void CancelDrag();
    void RegisterRepoRow(View view, Guid groupId, Guid repoId);
    void RegisterGroupHeader(View view, Guid groupId);
    void RegisterGroupSection(View view, Guid groupId);
    void Unregister(View view);
}

public sealed class DragController : IDragController
{
    private enum TargetKind { Repo, Header, Section }

    private sealed record Registration(TargetKind Kind, Guid GroupId, Guid RepoId);

    private readonly IRepoRegistry _registry;
    private readonly Dictionary<View, Registration> _registrations = new();

    public DragController(IRepoRegistry registry)
    {
        _registry = registry;
    }

    public State<DragSession?> Session { get; } = new(null);
    public State<DropTarget?> Target { get; } = new(null);

    public void StartDrag(Repo source, PointF mouse)
    {
        Session.Value = new DragSession { Source = source, MousePosition = mouse };
        Target.Value = null;
    }

    public void UpdateDrag(PointF mouse)
    {
        var session = Session.Value;
        if (session is null) return;
        session.MousePosition = mouse;
        Target.Value = ResolveTarget(mouse, session.Source.Id);
    }

    public void CompleteDrag()
    {
        var session = Session.Value;
        var target = Target.Value;
        Session.Value = null;
        Target.Value = null;
        if (session is null || target is null) return;
        _registry.MoveRepo(session.Source.Id, target.GroupId, target.InsertIndex);
    }

    public void CancelDrag()
    {
        Session.Value = null;
        Target.Value = null;
    }

    public void RegisterRepoRow(View view, Guid groupId, Guid repoId)
    {
        _registrations[view] = new Registration(TargetKind.Repo, groupId, repoId);
    }

    public void RegisterGroupHeader(View view, Guid groupId)
    {
        _registrations[view] = new Registration(TargetKind.Header, groupId, Guid.Empty);
    }

    public void RegisterGroupSection(View view, Guid groupId)
    {
        _registrations[view] = new Registration(TargetKind.Section, groupId, Guid.Empty);
    }

    public void Unregister(View view) => _registrations.Remove(view);

    private DropTarget? ResolveTarget(PointF mouse, Guid sourceRepoId)
    {
        View? hitRepoView = null;
        Registration? hitRepoReg = null;
        foreach (var (view, reg) in _registrations)
        {
            if (reg.Kind != TargetKind.Repo) continue;
            if (!view.Position.ContainsPoint(mouse)) continue;
            hitRepoView = view;
            hitRepoReg = reg;
            break;
        }

        if (hitRepoView is not null && hitRepoReg is not null)
        {
            var pos = hitRepoView.Position;
            var midY = pos.Bottom + pos.Height * 0.5f;
            var insertAbove = mouse.Y > midY;
            var group = FindGroup(hitRepoReg.GroupId);
            if (group is null) return null;
            var currentIndex = group.RepoIds.IndexOf(hitRepoReg.RepoId);
            if (currentIndex < 0) return null;
            var insertIndex = insertAbove ? currentIndex : currentIndex + 1;
            var indicatorY = insertAbove ? pos.Top : pos.Bottom;
            return new DropTarget
            {
                GroupId = hitRepoReg.GroupId,
                InsertIndex = insertIndex,
                IndicatorBounds = new RectF(pos.Left, indicatorY - 1, pos.Width, 2),
            };
        }

        View? hitHeaderView = null;
        Registration? hitHeaderReg = null;
        foreach (var (view, reg) in _registrations)
        {
            if (reg.Kind != TargetKind.Header) continue;
            if (!view.Position.ContainsPoint(mouse)) continue;
            hitHeaderView = view;
            hitHeaderReg = reg;
            break;
        }

        if (hitHeaderView is not null && hitHeaderReg is not null)
        {
            var pos = hitHeaderView.Position;
            return new DropTarget
            {
                GroupId = hitHeaderReg.GroupId,
                InsertIndex = 0,
                IndicatorBounds = new RectF(pos.Left, pos.Bottom - 1, pos.Width, 2),
            };
        }

        foreach (var (view, reg) in _registrations)
        {
            if (reg.Kind != TargetKind.Section) continue;
            if (!view.Position.ContainsPoint(mouse)) continue;
            var group = FindGroup(reg.GroupId);
            if (group is null) continue;
            var pos = view.Position;
            return new DropTarget
            {
                GroupId = reg.GroupId,
                InsertIndex = group.RepoIds.Count,
                IndicatorBounds = new RectF(pos.Left, pos.Bottom - 1, pos.Width, 2),
            };
        }

        return null;
    }

    private Group? FindGroup(Guid groupId)
    {
        foreach (var group in _registry.Groups)
        {
            if (group.Id == groupId) return group;
        }
        return null;
    }
}
