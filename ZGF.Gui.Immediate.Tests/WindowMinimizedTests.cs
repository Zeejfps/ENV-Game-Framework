namespace ZGF.Gui.Immediate.Tests;

public sealed class WindowMinimizedTests
{
    private readonly UiFrames _frames = new(new DefaultUiState(), body: static _ => { });

    private bool Minimized() => _frames.Frame(ui => ui.Input.IsMinimized);

    [Fact]
    public void The_window_stays_minimized_from_frame_to_frame_until_it_is_restored()
    {
        Assert.False(Minimized());

        _frames.Minimized(true);
        Assert.True(Minimized());
        Assert.True(Minimized());

        _frames.Minimized(false);
        Assert.False(Minimized());
    }
}
