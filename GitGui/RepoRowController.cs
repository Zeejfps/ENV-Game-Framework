using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Tests;
using ZGF.KeyboardModule;

namespace GitGui;

public sealed class RepoRowController : KeyboardMouseController
{
    private const float DragThresholdSq = 6f * 6f;

    private readonly Repo _repo;
    private readonly IRepoRegistry _registry;
    private readonly Action<bool> _onHoverChanged;
    private readonly Func<PointF, IReadOnlyList<RepoBarContextMenu.Item>> _buildMenuItems;

    private View? _view;
    private IDragController? _dragController;
    private InputSystem? _inputSystem;

    private bool _pressed;
    private bool _dragging;
    private PointF _pressPoint;

    public RepoRowController(
        Repo repo,
        IRepoRegistry registry,
        Action<bool> onHoverChanged,
        Func<PointF, IReadOnlyList<RepoBarContextMenu.Item>> buildMenuItems)
    {
        _repo = repo;
        _registry = registry;
        _onHoverChanged = onHoverChanged;
        _buildMenuItems = buildMenuItems;
    }

    protected override void OnAttachedToContext(View view, Context context)
    {
        _view = view;
        _dragController = context.Get<IDragController>();
        _inputSystem = context.Get<InputSystem>();

        var group = _registry.FindGroupContaining(_repo.Id);
        if (group is not null)
        {
            _dragController?.RegisterRepoRow(view, group.Id, _repo.Id);
        }
    }

    protected override void OnDetachedFromContext(View view, Context context)
    {
        _dragController?.Unregister(view);
        if (_pressed || _dragging)
        {
            _dragController?.CancelDrag();
            _inputSystem?.Blur(this);
        }
        _view = null;
        _dragController = null;
        _inputSystem = null;
    }

    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        if (_dragging) return;
        _onHoverChanged(true);
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        if (_dragging) return;
        _onHoverChanged(false);
    }

    public override void OnMouseMoved(ref MouseMoveEvent e)
    {
        if (!_pressed) return;

        if (!_dragging)
        {
            var dx = e.Mouse.Point.X - _pressPoint.X;
            var dy = e.Mouse.Point.Y - _pressPoint.Y;
            if (dx * dx + dy * dy < DragThresholdSq) return;

            _dragging = true;
            _onHoverChanged(false);
            _dragController?.StartRepoDrag(_repo, e.Mouse.Point);
            e.Consume();
            return;
        }

        _dragController?.UpdateDrag(e.Mouse.Point);
        e.Consume();
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Bubbling) return;

        if (e.Button == MouseButton.Right && e.State == InputState.Pressed)
        {
            if (_dragging) return;
            var items = _buildMenuItems(e.Mouse.Point);
            if (items.Count > 0 && Context is not null)
            {
                RepoBarContextMenu.Show(Context, e.Mouse.Point, items);
                e.Consume();
            }
            return;
        }

        if (e.Button != MouseButton.Left) return;

        if (e.State == InputState.Pressed)
        {
            _pressed = true;
            _dragging = false;
            _pressPoint = e.Mouse.Point;
            _inputSystem?.StealFocus(this);
            e.Consume();
            return;
        }

        if (e.State == InputState.Released)
        {
            if (!_pressed) return;
            _pressed = false;
            if (_dragging)
            {
                _dragging = false;
                _dragController?.CompleteDrag();
            }
            else
            {
                _registry.SetActive(_repo.Id);
            }
            _inputSystem?.Blur(this);
            e.Consume();
        }
    }

    public override void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
        if (!_dragging) return;
        if (e.State != InputState.Pressed) return;
        if (e.Key != KeyboardKey.Escape) return;
        _dragging = false;
        _pressed = false;
        _dragController?.CancelDrag();
        _inputSystem?.Blur(this);
        e.Consume();
    }

    public override void OnFocusLost()
    {
        if (_dragging)
        {
            _dragController?.CancelDrag();
            _dragging = false;
        }
        _pressed = false;
    }
}
