namespace ZGF.Gui.Immediate.Tests;

public sealed class QuitTests
{
    [Fact]
    public void Quit_is_requested_only_once_asked_for()
    {
        var frames = new UiFrames(new DefaultUiState());

        Assert.False(frames.Run(0f, ui => ui.QuitRequested));
        Assert.True(frames.Run(0f, ui =>
        {
            ui.Quit();
            return ui.QuitRequested;
        }));
    }
}
