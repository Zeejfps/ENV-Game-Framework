using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

internal static class Shortcuts
{
    private const KeyModifiers Chord = KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt;

    /// <summary>The key that analyzes what the focused view has picked: the tree's node, the code's
    /// reference at the caret, the analyzer's result.</summary>
    public const string Analyze = "Ctrl+R";

    /// <summary>Whether Ctrl+R was pressed this frame, for the focused view to analyze its pick.</summary>
    public static bool AnalyzePressed(Ui ui)
    {
        foreach (var stroke in ui.Input.Keys)
        {
            if (stroke.Key == KeyboardKey.R && (stroke.Modifiers & Chord) == KeyModifiers.Control)
                return true;
        }
        return false;
    }

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
                    if (w.Tabs.Active is { } tab) w.CloseTab(tab);
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
