using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

internal static class Shortcuts
{
    private const KeyModifiers Chord = KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt;

    public static void Handle(Ui ui, Workspace w)
    {
        if (w.Picker.IsOpen || w.Errors.IsOpen) return;

        var input = ui.Input;
        if (input.WasPressed(PointerButton.Back))
            w.GoBack();
        if (input.WasPressed(PointerButton.Forward))
            w.GoForward();

        var keys = input.Keys;
        for (var i = 0; i < keys.Count; i++)
        {
            var stroke = keys[i];
            switch (stroke.Modifiers & Chord, stroke.Key)
            {
                case (KeyModifiers.Control | KeyModifiers.Shift, KeyboardKey.F):
                    w.OpenSearch();
                    continue;
                case (KeyModifiers.Alt, KeyboardKey.LeftArrow):
                    w.GoBack();
                    continue;
                case (KeyModifiers.Alt, KeyboardKey.RightArrow):
                    w.GoForward();
                    continue;
            }
            if (ui.WantsTextInput) continue;

            switch (stroke.Modifiers & Chord, stroke.Key)
            {
                case (KeyModifiers.Control, KeyboardKey.O):
                    MainMenuView.OpenFile(ui, w);
                    break;
                case (KeyModifiers.None, KeyboardKey.F5):
                    w.Refresh();
                    break;
                case (KeyModifiers.Control, KeyboardKey.W):
                    if (w.ActiveTab is { } tab) w.CloseTab(tab);
                    break;
                case (KeyModifiers.Control, KeyboardKey.Tab):
                    w.CycleTab(1);
                    break;
                case (KeyModifiers.Control | KeyModifiers.Shift, KeyboardKey.Tab):
                    w.CycleTab(-1);
                    break;
            }
        }
    }
}
