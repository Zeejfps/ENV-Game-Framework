namespace ZGF.Gui.Immediate.Tests;

public sealed class PointerTests
{
    private static readonly List<Interaction> Seen = new();

    private static void Two(Ui ui)
    {
        using (ui.Row())
        {
            foreach (var label in new[] { "a", "b" })
            {
                var i = ui.Interact(label);
                Seen.Add(i);
                ui.Leaf(i.Id, new Box { Width = 100f, Height = 40f });
            }
        }
    }

    [Theory]
    [InlineData(PointerButton.Right)]
    [InlineData(PointerButton.Middle)]
    [InlineData(PointerButton.Back)]
    [InlineData(PointerButton.Forward)]
    public void Other_buttons_click_the_element_pressed_and_released_on(PointerButton button)
    {
        var frames = new UiFrames(new DefaultUiState());
        Frame(frames);

        frames.MovePointer(50f, 30f);
        Frame(frames);
        frames.Press(button);
        var pressed = Frame(frames);
        frames.Release(button);
        var released = Frame(frames);

        Assert.False(pressed.ClickedWith(button));
        Assert.True(released.ClickedWith(button));
        Assert.False(released.Clicked);
        Assert.False(released.Pressed);
    }

    [Fact]
    public void A_release_over_another_element_is_no_click()
    {
        var frames = new UiFrames(new DefaultUiState());
        Frame(frames);

        frames.MovePointer(50f, 30f);
        Frame(frames);
        frames.Press(PointerButton.Right);
        Frame(frames);
        frames.MovePointer(160f, 30f);
        frames.Release(PointerButton.Right);
        Frame(frames);

        Assert.DoesNotContain(Seen, i => i.ClickedWith(PointerButton.Right));
    }

    [Fact]
    public void Quick_presses_on_the_same_element_count_up_to_a_double_click()
    {
        var frames = new UiFrames(new DefaultUiState());
        Frame(frames);
        frames.MovePointer(50f, 30f);
        Frame(frames);

        var first = PressRelease(frames);
        var second = PressRelease(frames);
        frames.Wait(1.0);
        var late = PressRelease(frames);

        Assert.Equal(1, first.ClickCount);
        Assert.False(first.DoubleClicked);
        Assert.Equal(2, second.ClickCount);
        Assert.True(second.DoubleClicked);
        Assert.Equal(1, late.ClickCount);
    }

    [Fact]
    public void A_second_press_on_another_element_starts_a_new_count()
    {
        var frames = new UiFrames(new DefaultUiState());
        Frame(frames);
        frames.MovePointer(50f, 30f);
        Frame(frames);
        PressRelease(frames);

        frames.MovePointer(160f, 30f);
        Frame(frames);
        frames.Press();
        Frame(frames);
        var b = Seen[1];
        frames.Release();
        Frame(frames);

        Assert.True(b.Pressed);
        Assert.Equal(1, b.ClickCount);
    }

    [Fact]
    public void Focus_that_takes_no_text_leaves_app_shortcuts_alone()
    {
        var (navigating, typing) = new UiFrames(new DefaultUiState()).Run(0f, ui =>
        {
            ui.Focus(1, takesText: false);
            var navigatingWants = ui.WantsTextInput;
            ui.Focus(2);
            return (navigatingWants, ui.WantsTextInput);
        });

        Assert.False(navigating);
        Assert.True(typing);
    }

    private static Interaction PressRelease(UiFrames frames)
    {
        frames.Press();
        var pressed = Frame(frames);
        frames.Release();
        Frame(frames);
        return pressed;
    }

    private static Interaction Frame(UiFrames frames)
    {
        Seen.Clear();
        frames.Run(1f / 60f, Two);
        return Seen[0];
    }
}
