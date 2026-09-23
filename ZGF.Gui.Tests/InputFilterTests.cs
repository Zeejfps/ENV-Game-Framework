using ZGF.Desktop.Input;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

/// <summary>
/// A filter sees every key before any controller does, the focused one included, so a gesture
/// spanning several keys can be recognised wherever focus is. One that claims a key stops it there.
/// </summary>
public class InputFilterTests
{
    private sealed class Recording : KeyboardMouseController
    {
        public List<KeyboardKey> Keys { get; } = new();
        public bool ConsumeAll { get; init; }

        public override void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
        {
            Keys.Add(e.Key);
            if (ConsumeAll) e.Consume();
        }
    }

    private sealed class Filter : IInputFilter
    {
        public List<(KeyboardKey Key, InputState State)> Keys { get; } = new();
        public List<InputState> Buttons { get; } = new();
        public int FocusLosses { get; private set; }
        public KeyboardKey? Claim { get; init; }

        public void OnKey(ref KeyboardKeyEvent e)
        {
            Keys.Add((e.Key, e.State));
            if (e.Key == Claim) e.Consume();
        }

        public void OnMouseButton(in MouseButtonEvent e) => Buttons.Add(e.State);

        public void OnWindowFocusLost() => FocusLosses++;
    }

    private static void Key(InputSystem input, KeyboardKey key, InputState state)
    {
        var e = new KeyboardKeyEvent { Key = key, State = state, Modifiers = InputModifiers.None, Phase = EventPhase.Capturing };
        input.SendKeyboardKeyEvent(ref e);
    }

    [Fact]
    public void AFilterSeesKeysAFocusedControllerConsumes_PressesAndReleases()
    {
        var input = new InputSystem();
        var focused = new Recording { ConsumeAll = true };
        var view = new RectView();
        view.UseController(input, focused);
        view.Mount();
        input.StealFocus(focused);
        var filter = new Filter();
        input.AddFilter(filter);

        Key(input, KeyboardKey.LeftShift, InputState.Pressed);
        Key(input, KeyboardKey.LeftShift, InputState.Released);

        Assert.Equal([(KeyboardKey.LeftShift, InputState.Pressed), (KeyboardKey.LeftShift, InputState.Released)], filter.Keys);
        Assert.Equal([KeyboardKey.LeftShift, KeyboardKey.LeftShift], focused.Keys);
    }

    [Fact]
    public void AKeyAFilterClaims_ReachesNoController()
    {
        var input = new InputSystem();
        var focused = new Recording();
        var view = new RectView();
        view.UseController(input, focused);
        view.Mount();
        input.StealFocus(focused);
        input.AddFilter(new Filter { Claim = KeyboardKey.R });

        Key(input, KeyboardKey.R, InputState.Pressed);
        Key(input, KeyboardKey.T, InputState.Pressed);

        Assert.Equal([KeyboardKey.T], focused.Keys);
    }

    [Fact]
    public void AFilterWatchesMouseButtonsAndFocusLoss_AndCanBeRemoved()
    {
        var input = new InputSystem();
        var filter = new Filter();
        input.AddFilter(filter);

        var press = new MouseButtonEvent { Mouse = new Mouse(), Button = MouseButton.Left, State = InputState.Pressed, Phase = EventPhase.Capturing };
        input.SendMouseButtonEvent(ref press);
        input.NotifyWindowFocusLost();
        input.RemoveFilter(filter);
        Key(input, KeyboardKey.A, InputState.Pressed);

        Assert.Equal([InputState.Pressed], filter.Buttons);
        Assert.Equal(1, filter.FocusLosses);
        Assert.Empty(filter.Keys);
    }
}
