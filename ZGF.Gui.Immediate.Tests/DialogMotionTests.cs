using ZGF.Gui.Immediate.Widgets;
using ZGF.Gui.Testing;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>
/// A dialog comes in and goes rather than popping: on the frame it opens its backdrop is clear and its card drawn
/// faded, lowered and smaller, and over <see cref="UiWidgets.DialogInSeconds"/> both come fully into place. It takes
/// input from the first frame, and it comes in again each time it opens. Recorded on once closed, it goes the same
/// way back over <see cref="UiWidgets.DialogOutSeconds"/>, its content still drawn but taking no input; one that
/// was not open just before draws nothing.
/// </summary>
public sealed class DialogMotionTests
{
    private const float Step = 1f / 60f;

    private readonly UiFrames _frames;
    private bool _open;
    private int _okClicks;

    public DialogMotionTests() =>
        _frames = new UiFrames(new DefaultUiState(), width: 800f, height: 600f, body: Body) { FrameTime = Step };

    private void Body(Ui ui)
    {
        using var dialog = ui.Dialog("dialog", "Settings", ref _open);
        if (dialog.IsOpen && ui.Button("OK"))
            _okClicks++;
    }

    private RecordedRect Card() =>
        _frames.Canvas.Rects.Single(r => r.Inputs.Style.BackgroundColor == UiTheme.Dark.SurfaceRaised);

    /// <summary>How opaque the backdrop is drawn; 0 when it is clear, which is not drawn at all.</summary>
    private uint ScrimAlpha()
    {
        var scrim = _frames.Canvas.Rects.SingleOrDefault(r => (r.Inputs.Style.BackgroundColor & 0x00FFFFFFu) == (UiTheme.Dark.Scrim & 0x00FFFFFFu)
            && r.Inputs.Style.BackgroundColor != UiTheme.Dark.SurfaceRaised);
        return scrim == null ? 0u : scrim.Inputs.Style.BackgroundColor >> 24;
    }

    private void Open()
    {
        _frames.Frame();
        _open = true;
        _frames.Frame();
    }

    [Fact]
    public void On_the_frame_it_opens_its_card_is_faded_lowered_and_smaller_on_a_clear_backdrop()
    {
        Open();

        var card = Card();
        Assert.True(card.Opacity < 0.1f);
        Assert.True(card.TranslationY > 0f);
        Assert.True(card.ScaleX < 1f);
        Assert.True(ScrimAlpha() < 0x10u);
    }

    [Fact]
    public void Once_it_has_come_in_it_is_fully_in_place()
    {
        Open();
        _frames.Frame((int)MathF.Ceiling(UiWidgets.DialogInSeconds / Step) + 1);

        var card = Card();
        Assert.Equal(1f, card.Opacity);
        Assert.Equal(0f, card.TranslationY);
        Assert.Equal(1f, card.ScaleX);
        Assert.Equal(UiTheme.Dark.Scrim >> 24, ScrimAlpha());
    }

    [Fact]
    public void It_takes_input_while_it_comes_in()
    {
        Open();

        _frames.ClickOn("OK");

        Assert.Equal(1, _okClicks);
    }

    private void ComeIn() => _frames.Frame((int)MathF.Ceiling(UiWidgets.DialogInSeconds / Step) + 1);

    [Fact]
    public void Recorded_on_once_closed_it_goes_faded_and_sinking_with_its_content()
    {
        Open();
        ComeIn();

        _open = false;
        _frames.Frame(2);

        var card = Card();
        Assert.True(card.Opacity is > 0f and < 1f);
        Assert.True(card.TranslationY > 0f);
        Assert.True(_frames.Drew("OK"));
    }

    [Fact]
    public void Once_it_has_gone_nothing_of_it_is_drawn()
    {
        Open();
        ComeIn();

        _open = false;
        _frames.Frame((int)MathF.Ceiling(UiWidgets.DialogOutSeconds / Step) + 1);

        Assert.False(_frames.Drew("Settings"));
        Assert.False(_frames.Drew("OK"));
    }

    [Fact]
    public void As_it_goes_it_takes_no_input()
    {
        Open();
        ComeIn();
        var ok = _frames.RectOf(ui => ui.GetId("OK"));

        _open = false;
        _frames.Frame();
        _frames.Click(ok.X + ok.Width / 2f, ok.Y + ok.Height / 2f);

        Assert.Equal(0, _okClicks);
    }

    [Fact]
    public void A_dialog_that_was_never_open_draws_nothing()
    {
        _frames.Frame(3);

        Assert.False(_frames.Drew("Settings"));
    }

    [Fact]
    public void It_comes_in_again_each_time_it_opens()
    {
        Open();
        _frames.Frame((int)MathF.Ceiling(UiWidgets.DialogInSeconds / Step) + 1);
        _open = false;
        _frames.Frame();

        _open = true;
        _frames.Frame();

        Assert.True(Card().Opacity < 0.1f);
    }
}
