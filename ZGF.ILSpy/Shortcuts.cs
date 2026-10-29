using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.KeyboardModule;
using static ZGF.Desktop.ShortcutModifiers;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.Exports;
using static ZGF.ILSpy.Navigation;
using static ZGF.ILSpy.DocumentPanels;
using static ZGF.ILSpy.ScreenLayout;

namespace ZGF.ILSpy;

/// <summary>Every key that does something in the app, matched and labelled from here: the app-wide
/// ones in <see cref="Bindings"/>, which <see cref="Handle"/> runs, and those the focused view handles
/// itself. Command is Ctrl, but ⌘ on macOS.</summary>
internal static class Shortcuts
{
    /// <summary>Analyzes what the focused view has picked: the tree's node, the code's reference at
    /// the caret, the analyzer's result.</summary>
    public static readonly Shortcut Analyze = Shortcut.Command(KeyboardKey.R);

    public static readonly Shortcut Open = Shortcut.Command(KeyboardKey.O);
    public static readonly Shortcut SaveCode = Shortcut.Command(KeyboardKey.S);
    public static readonly Shortcut Export = Shortcut.Command(KeyboardKey.E, Shift);
    public static readonly Shortcut Search = Shortcut.Command(KeyboardKey.F, Shift);
    public static readonly Shortcut OmnibarSearch = Shortcut.Command(KeyboardKey.L);
    public static readonly Shortcut ReloadAll = Shortcut.Key(KeyboardKey.F5);
    public static readonly Shortcut CloseTab = Shortcut.Command(KeyboardKey.W);
    public static readonly Shortcut NextTab = Shortcut.Key(KeyboardKey.Tab, Control);
    public static readonly Shortcut PreviousTab = Shortcut.Key(KeyboardKey.Tab, Control | Shift);

    // macOS goes back and forward with ⌘[ and ⌘]: Option and the arrows move by word in a text field.
    public static readonly Shortcut Back = OperatingSystem.IsMacOS()
        ? Shortcut.Command(KeyboardKey.LeftBracket)
        : Shortcut.Key(KeyboardKey.LeftArrow, Alt);
    public static readonly Shortcut Forward = OperatingSystem.IsMacOS()
        ? Shortcut.Command(KeyboardKey.RightBracket)
        : Shortcut.Key(KeyboardKey.RightArrow, Alt);

    public static readonly Shortcut ZoomIn = Shortcut.Command(KeyboardKey.Equals);
    public static readonly Shortcut ZoomOut = Shortcut.Command(KeyboardKey.Minus);
    public static readonly Shortcut ZoomReset = Shortcut.Command(KeyboardKey.Alpha0);

    /// <summary>What zooms the code, the labelled keys first: also Command with Shift and =, which
    /// reads as Command and +, and the keypad's.</summary>
    public static readonly Shortcut[] ZoomInKeys = [ZoomIn, Shortcut.Command(KeyboardKey.Equals, Shift), Shortcut.Command(KeyboardKey.NumpadAdd)];
    public static readonly Shortcut[] ZoomOutKeys = [ZoomOut, Shortcut.Command(KeyboardKey.NumpadSubtract)];
    public static readonly Shortcut[] ZoomResetKeys = [ZoomReset, Shortcut.Command(KeyboardKey.Numpad0)];

    public static readonly Shortcut Copy = Shortcut.Command(KeyboardKey.C);
    public static readonly Shortcut CopyPlainText = Shortcut.Command(KeyboardKey.C, Shift);
    public static readonly Shortcut SelectAll = Shortcut.Command(KeyboardKey.A);
    public static readonly Shortcut Find = Shortcut.Command(KeyboardKey.F);
    public static readonly Shortcut FindNext = Shortcut.Key(KeyboardKey.F3);
    public static readonly Shortcut FindPrevious = Shortcut.Key(KeyboardKey.F3, Shift);

    // The fold chords take Control itself on every platform: ⌘M minimizes the window on macOS.
    public static readonly Shortcut ToggleFold = Shortcut.Key(KeyboardKey.M, Control).Then(KeyboardKey.M);
    public static readonly Shortcut ToggleAllFolds = Shortcut.Key(KeyboardKey.M, Control).Then(KeyboardKey.L);
    public static readonly Shortcut CollapseFold = Shortcut.Command(KeyboardKey.LeftBracket, Shift);
    public static readonly Shortcut ExpandFold = Shortcut.Command(KeyboardKey.RightBracket, Shift);

    public static readonly Shortcut GoToToken = Shortcut.Command(KeyboardKey.G);
    public static readonly Shortcut Rename = Shortcut.Key(KeyboardKey.F2);
    public static readonly Shortcut Delete = Shortcut.Key(KeyboardKey.Delete);
    public static readonly Shortcut Enter = Shortcut.Key(KeyboardKey.Enter);
    public static readonly Shortcut Space = Shortcut.Key(KeyboardKey.Space);
    public static readonly Shortcut ContextMenu = Shortcut.Key(KeyboardKey.F10, Shift);
    public static readonly Shortcut CloseWindow = Shortcut.Key(KeyboardKey.F4, Alt);

    // macOS's app and Window menus, which the system handles.
    public static readonly Shortcut Settings = Shortcut.Command(KeyboardKey.Comma);
    public static readonly Shortcut Hide = Shortcut.Command(KeyboardKey.H);
    public static readonly Shortcut HideOthers = Shortcut.Command(KeyboardKey.H, Alt);
    public static readonly Shortcut Quit = Shortcut.Command(KeyboardKey.Q);
    public static readonly Shortcut Minimize = Shortcut.Command(KeyboardKey.M);

    /// <summary>An app-wide shortcut and what it does; one that works <see cref="WhileTyping"/> runs
    /// even while a text field has focus.</summary>
    private readonly record struct Binding(Shortcut Keys, Action<Ui<UiState>, AppState> Run, bool WhileTyping = false);

    private static readonly Binding[] Bindings =
    [
        new(Search, static (ui, _) => OpenSearch(ui.State), WhileTyping: true),
        new(OmnibarSearch, static (ui, _) => SearchInOmnibarUnlessFolding(ui), WhileTyping: true),
        new(Back, static (_, appState) => GoBack(appState), WhileTyping: true),
        new(Forward, static (_, appState) => GoForward(appState), WhileTyping: true),
        new(Open, static (ui, appState) => MainMenuView.OpenFile(ui, appState)),
        new(SaveCode, static (ui, appState) => SaveDialogs.PickActiveCodeFile(ui, appState)),
        new(Export, static (ui, appState) => ui.State.ExportSetup.Open(appState.Assemblies.Assemblies, FindActiveAssembly(appState))),
        new(ReloadAll, static (ui, appState) => RefreshAssemblies(appState, ui.State.Tree.ExpandedPaths())),
        new(CloseTab, static (ui, _) => CloseActiveTab(ui.State)),
        new(NextTab, static (ui, _) => CycleTab(ui.State, 1)),
        new(PreviousTab, static (ui, _) => CycleTab(ui.State, -1)),
    ];

    /// <summary>Runs the app-wide shortcuts pressed this frame, and goes back and forward with the
    /// pointer's side buttons.</summary>
    public static void Handle(Ui<UiState> ui, AppState appState)
    {
        var input = ui.Input;
        if (input.WasPressed(PointerButton.Back))
            GoBack(appState);
        if (input.WasPressed(PointerButton.Forward))
            GoForward(appState);

        foreach (var binding in Bindings)
        {
            if (ui.WantsTextInput && !binding.WhileTyping)
                continue;
            if (ui.IsPressed(binding.Keys))
                binding.Run(ui, appState);
        }
    }

    /// <summary>Searches in the omnibar, unless the key ends Ctrl+M, Ctrl+L in the code, which folds it.</summary>
    private static void SearchInOmnibarUnlessFolding(Ui<UiState> ui)
    {
        if (ui.IsPressed(ToggleAllFolds))
            return;
        SearchInOmnibar(ui.State);
    }

    /// <summary>Whether any of <paramref name="shortcuts"/> was pressed this frame.</summary>
    public static bool AnyPressed(Ui<UiState> ui, Shortcut[] shortcuts)
    {
        foreach (var shortcut in shortcuts)
        {
            if (ui.IsPressed(shortcut))
                return true;
        }
        return false;
    }
}
