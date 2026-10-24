using ZGF.Geometry;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class DragTests
{
    private static readonly PointF Source = new(10f, 10f);

    private readonly UiFrames _frames;
    private readonly List<string> _log = new();

    public DragTests() => _frames = new UiFrames(new DefaultUiState(), body: Body);

    private void Body(Ui ui)
    {
        var source = ui.Interact("source");
        if (ui.ShouldBeginDrag(source))
            ui.BeginDrag("payload");
        ui.Leaf(source.Id, new Box { Width = 50f, Height = 50f });

        var target = ui.Interact("target");
        if (ui.IsPointerOver(target.Id) && ui.TryDrop<string>(out var dropped))
            _log.Add(dropped);
        ui.Leaf(target.Id, new Box { Width = 50f, Height = 50f });
    }

    private bool Dragging() => _frames.Frame(ui => ui.IsDragging);

    [Fact]
    public void A_drag_starts_once_the_pointer_moves_past_the_threshold()
    {
        _frames.Drag(Source);

        _frames.MovePointer(13f, 10f);
        Assert.False(Dragging());
        _frames.MovePointer(20f, 10f);
        Assert.True(Dragging());
    }

    [Fact]
    public void The_target_under_the_pointer_takes_the_payload_on_release()
    {
        _frames.Drag(Source, new PointF(20f, 10f), new PointF(10f, 60f));

        _frames.Release().Frame();

        Assert.Equal(["payload"], _log);
        Assert.False(Dragging());
    }

    [Fact]
    public void A_drop_nobody_takes_ends_the_drag()
    {
        _frames.Drag(Source, new PointF(200f, 200f));

        _frames.Release().Frame();

        Assert.False(Dragging());
        Assert.Empty(_log);
    }

    [Fact]
    public void Losing_focus_cancels_a_drag_without_dropping()
    {
        _frames.Drag(Source, new PointF(10f, 60f));

        _frames.LoseFocus().Frame();

        Assert.Empty(_log);
        Assert.False(Dragging());
    }
}

public sealed class StateScopeTests
{
    private sealed class Counter
    {
        public int Value;
    }

    private readonly UiFrames _frames;
    private bool _shown = true;
    private bool _kept = true;

    public StateScopeTests() => _frames = new UiFrames(new DefaultUiState(), body: ui => Body(ui));

    private Counter Body(Ui ui)
    {
        Counter counter = null!;
        if (_shown)
        {
            using (ui.StateScope("panel"))
                counter = ui.GetState<Counter>(ui.GetId("counter"));
        }
        else if (_kept)
        {
            ui.KeepState("panel");
        }
        return counter;
    }

    private void HideFor(int frames)
    {
        _shown = false;
        _frames.Frame(frames);
        _shown = true;
    }

    [Fact]
    public void State_inside_a_kept_scope_outlives_frames_it_is_not_recorded()
    {
        _frames.Run(0f, Body).Value = 42;

        HideFor(1500);

        Assert.Equal(42, _frames.Run(0f, Body).Value);
    }

    [Fact]
    public void State_inside_a_scope_nobody_keeps_is_dropped_like_any_other()
    {
        _frames.Run(0f, Body).Value = 42;
        _kept = false;

        HideFor(1500);

        Assert.Equal(0, _frames.Run(0f, Body).Value);
    }

    [Fact]
    public void A_state_scope_scopes_ids_like_Id()
    {
        var (scoped, plain) = _frames.Run(0f, ui =>
        {
            int a, b;
            using (ui.StateScope("panel"))
                a = ui.GetId("x");
            using (ui.Id("panel"))
                b = ui.GetId("x");
            return (a, b);
        });

        Assert.Equal(plain, scoped);
    }
}
