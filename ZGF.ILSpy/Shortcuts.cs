using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.KeyboardModule;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.Exports;
using static ZGF.ILSpy.Navigation;
using static ZGF.ILSpy.Dialogs;
using static ZGF.ILSpy.DocumentPanels;
using static ZGF.ILSpy.ScreenLayout;

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
    public const string CloseTab = "Ctrl+W";
    public const string ZoomIn = "Ctrl+=";
    public const string ZoomOut = "Ctrl+-";
    public const string ZoomReset = "Ctrl+0";

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

    public static void Handle(Ui<UiState> ui, AppState appState)
    {
        if (IsDialogOpen(ui.State)) return;

        var input = ui.Input;
        if (input.WasPressed(PointerButton.Back))
            GoBack(appState);
        if (input.WasPressed(PointerButton.Forward))
            GoForward(appState);

        var keys = input.Keys;
        for (var i = 0; i < keys.Count; i++)
        {
            var stroke = keys[i];
            switch (stroke.Modifiers & Chord, stroke.Key)
            {
                case (KeyModifiers.Control | KeyModifiers.Shift, KeyboardKey.F):
                    OpenSearch(ui.State);
                    continue;
                case (KeyModifiers.Alt, KeyboardKey.LeftArrow):
                    GoBack(appState);
                    continue;
                case (KeyModifiers.Alt, KeyboardKey.RightArrow):
                    GoForward(appState);
                    continue;
            }
            if (ui.WantsTextInput) continue;

            switch (stroke.Modifiers & Chord, stroke.Key)
            {
                case (KeyModifiers.Control, KeyboardKey.O):
                    MainMenuView.OpenFile(ui, appState);
                    break;
                case (KeyModifiers.Control, KeyboardKey.S):
                    SaveDialogs.PickActiveCodeFile(ui, appState);
                    break;
                case (KeyModifiers.Control | KeyModifiers.Shift, KeyboardKey.E):
                    ui.State.ExportSetup.Open(appState.Assemblies.Assemblies, FindActiveAssembly(appState));
                    break;
                case (KeyModifiers.None, KeyboardKey.F5):
                    RefreshAssemblies(appState, ui.State.Tree.ExpandedPaths());
                    break;
                case (KeyModifiers.Control, KeyboardKey.W):
                    CloseActiveTab(ui.State);
                    break;
                case (KeyModifiers.Control, KeyboardKey.Tab):
                    CycleTab(ui.State, 1);
                    break;
                case (KeyModifiers.Control | KeyModifiers.Shift, KeyboardKey.Tab):
                    CycleTab(ui.State, -1);
                    break;
            }
        }
    }
}
