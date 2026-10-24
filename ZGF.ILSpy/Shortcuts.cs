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

    public const string Open = "Ctrl+O";
    public const string SaveCode = "Ctrl+S";
    public const string Export = "Ctrl+Shift+E";
    public const string Search = "Ctrl+Shift+F";
    public const string Back = "Alt+Left";
    public const string Forward = "Alt+Right";
    public const string ReloadAll = "F5";

    /// <summary>Whether Ctrl+R was pressed this frame, for the focused view to analyze its pick.</summary>
    public static bool AnalyzePressed(Ui<UiState> ui)
    {
        foreach (var stroke in ui.Input.Keys)
        {
            if (stroke.Key == KeyboardKey.R && (stroke.Modifiers & Chord) == KeyModifiers.Control)
                return true;
        }
        return false;
    }

    /// <summary>Whether Delete was pressed this frame, for the focused view to remove its pick.</summary>
    public static bool DeletePressed(Ui<UiState> ui)
    {
        foreach (var stroke in ui.Input.Keys)
        {
            if (stroke.Key == KeyboardKey.Delete && (stroke.Modifiers & Chord) == KeyModifiers.None)
                return true;
        }
        return false;
    }

    public static void Handle(Ui<UiState> ui, AppState w)
    {
        if (ui.State.IsDialogOpen()) return;

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
                    ui.State.OpenSearch();
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
                case (KeyModifiers.Control, KeyboardKey.S):
                    SaveDialogs.PickActiveCodeFile(ui, w);
                    break;
                case (KeyModifiers.Control | KeyModifiers.Shift, KeyboardKey.E):
                    ui.State.OpenExportSetup(w.ActiveAssembly());
                    break;
                case (KeyModifiers.None, KeyboardKey.F5):
                    w.Refresh(ui.State.Tree.ExpandedPaths());
                    break;
                case (KeyModifiers.Control, KeyboardKey.W):
                    ui.State.CloseActiveTab();
                    break;
                case (KeyModifiers.Control, KeyboardKey.Tab):
                    ui.State.CycleTab(1);
                    break;
                case (KeyModifiers.Control | KeyModifiers.Shift, KeyboardKey.Tab):
                    ui.State.CycleTab(-1);
                    break;
            }
        }
    }
}
