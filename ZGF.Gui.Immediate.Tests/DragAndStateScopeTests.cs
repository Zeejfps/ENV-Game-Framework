using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class DragTests
{
    private readonly UiFrames _frames = new();
    private readonly List<string> _log = new();

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

    private void PressSource()
    {
        _frames.MovePointer(10f, 10f);
        _frames.Run(0f, Body);
        _frames.Press();
        _frames.Run(0f, Body);
    }

    private bool Dragging() => _frames.Run(0f, ui =>
    {
        Body(ui);
        return ui.IsDragging;
    });

    [Fact]
    public void A_drag_starts_once_the_pointer_moves_past_the_threshold()
    {
        PressSource();

        _frames.MovePointer(13f, 10f);
        Assert.False(Dragging());
        _frames.MovePointer(20f, 10f);
        Assert.True(Dragging());
    }

    [Fact]
    public void The_target_under_the_pointer_takes_the_payload_on_release()
    {
        PressSource();
        _frames.MovePointer(20f, 10f);
        _frames.Run(0f, Body);
        _frames.MovePointer(10f, 60f);
        _frames.Run(0f, Body);

        _frames.Release();
        _frames.Run(0f, Body);

        Assert.Equal(["payload"], _log);
        Assert.False(Dragging());
    }

    [Fact]
    public void A_drop_nobody_takes_ends_the_drag()
    {
        PressSource();
        _frames.MovePointer(200f, 200f);
        _frames.Run(0f, Body);

        _frames.Release();
        _frames.Run(0f, Body);

        Assert.False(Dragging());
        Assert.Empty(_log);
    }

    [Fact]
    public void Losing_focus_cancels_a_drag_without_dropping()
    {
        PressSource();
        _frames.MovePointer(10f, 60f);
        _frames.Run(0f, Body);

        _frames.LoseFocus();
        _frames.Run(0f, Body);

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

    private readonly UiFrames _frames = new();
    private bool _shown = true;
    private bool _kept = true;

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
        for (var i = 0; i < frames; i++)
            _frames.Run(0f, Body);
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
