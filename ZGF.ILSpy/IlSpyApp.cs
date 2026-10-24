using ZGF.AppUtils;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using static ZGF.ILSpy.Analysis;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.AssemblyLists;
using static ZGF.ILSpy.Docking;
using static ZGF.ILSpy.Settings;

namespace ZGF.ILSpy;

public sealed class IlSpyApp : ImmediateGuiApp<UiState>
{
    private const string MonoFontFile = "JetBrainsMono-Regular.ttf";
    private const string ItalicFontFile = "Inter-Italic.ttf";

    private readonly AppState _appState;
    private readonly List<string> _startupAssemblies;

    internal IlSpyApp(
        AppState appState,
        UiState uiState,
        List<string> startupAssemblies) : base(uiState)
    {
        _appState = appState;
        _startupAssemblies = startupAssemblies;
    }

    protected override void OnStartup(ImmediateGuiAppHost<UiState> host)
    {
        LoadAndRegisterFonts(host);
        OpenStartupAssemblies();
        StartRedrawingOnAssemblyChanges(host);
        RestoreAnalyses(_appState);
    }

    protected override void OnUpdate(Ui<UiState> ui)
    {
        RenderFrame(ui, _appState);
        RunAppCommandsAndRedraw(ui, _appState);
        RequestUpdateIfViewCommandsPending(ui, _appState);
        SaveChangedSettings(ui, _appState);
    }

    protected override void OnShutdown(ImmediateGuiAppHost<UiState> host)
    {
        StopRedrawingOnAssemblyChanges(host);
        SaveOnExit(host.WindowBounds);
    }

    private static void RenderFrame(Ui<UiState> ui, AppState appState)
    {
        ui.State.StartFrame();
        MainView.Render(ui, appState);
        ui.State.FinishFrame();
    }

    /// <summary>Runs what the screen asked of the app state while it drew, and draws again to show it.</summary>
    private static void RunAppCommandsAndRedraw(Ui<UiState> ui, AppState appState)
    {
        if (ui.State.AppCommands.Count == 0)
            return;
        RunAppCommands(ui.State, appState);
        ui.RequestUpdate();
    }

    /// <summary>Runs the commands <paramref name="uiState"/> queued for the app state, in the order queued.</summary>
    internal static void RunAppCommands(UiState uiState, AppState appState)
    {
        var commands = uiState.AppCommands;
        while (commands.TryDequeue(out var command))
            Run(command, uiState, appState);
    }

    private static void Run(AppCommand command, UiState uiState, AppState appState)
    {
        switch (command)
        {
            case AppCommand.OpenTab open:
                OpenTabIn(uiState, appState, open.Panel, open.Focus);
                break;
            case AppCommand.CloseTab close:
                CloseTabAndFocusNext(uiState, appState, close.Tab);
                break;
            case AppCommand.FocusTab focus:
                FocusTabAndReveal(uiState, appState, focus.Tab);
                break;
        }
    }

    /// <summary>Opens an empty tab shown in <paramref name="panel"/>, unless the panel was closed meanwhile,
    /// and makes it the active tab when asked to or when the layout focuses its panel.</summary>
    private static void OpenTabIn(UiState uiState, AppState appState, string panel, bool focus)
    {
        if (!uiState.Layout.Contains(panel))
            return;
        var tab = OpenEmptyTab(appState);
        uiState.AddTabView(tab, panel);
        var becomesActive = focus || uiState.Layout.Focused == panel;
        if (becomesActive)
            FocusTabAndReveal(uiState, appState, tab);
    }

    private static void CloseTabAndFocusNext(UiState uiState, AppState appState, DocumentTab tab)
    {
        var wasActive = CloseTab(appState, tab);
        if (!wasActive)
            return;
        uiState.FocusNextTab();
    }

    private static void FocusTabAndReveal(UiState uiState, AppState appState, DocumentTab tab)
    {
        FocusTab(appState, tab);
        RevealInTree(uiState, appState, tab);
    }

    /// <summary>Reveals what <paramref name="tab"/> shows in the tree, when that is listed.</summary>
    private static void RevealInTree(UiState uiState, AppState appState, DocumentTab tab)
    {
        var node = tab.Node;
        if (node == null)
            return;
        if (!appState.IsListed(node))
            return;
        uiState.Tree.Reveal(node);
    }

    private static void LoadAndRegisterFonts(ImmediateGuiAppHost<UiState> host)
    {
        host.RegisterFont(LoadFont(MonoFonts.Regular, MonoFontFile));
        host.RegisterFont(LoadFont(TextFonts.Italic, ItalicFontFile));
        host.RegisterFont(LoadFont(LucideIcons.Family, LucideIcons.File));
    }

    private static FontFace LoadFont(string family, string file)
    {
        var data = EmbeddedAssets.LoadBytes(typeof(IlSpyApp).Assembly, file);
        return new FontFace(family, data);
    }

    private void StartRedrawingOnAssemblyChanges(ImmediateGuiAppHost<UiState> host)
    {
        _appState.Assemblies.Changed += host.Invalidate;
    }

    private void OpenStartupAssemblies()
    {
        RestoreActiveAssemblyList(_appState);
        OpenAssemblies(_appState, _startupAssemblies);
    }

    private void StopRedrawingOnAssemblyChanges(ImmediateGuiAppHost<UiState> host)
    {
        _appState.Assemblies.Changed -= host.Invalidate;
    }

    internal void SaveOnExit(WindowBounds window)
    {
        _appState.Settings.RememberWindow(window);
        UiState.HandOverViewSettings();
        SaveSettings(_appState);
    }
    
    private static void RequestUpdateIfViewCommandsPending(Ui<UiState> ui, AppState appState)
    {
        if (appState.ViewCommands.Count == 0)
            return;
        ui.RequestUpdate();
    }
    
    private static void SaveChangedSettings(Ui<UiState> ui, AppState appState)
    {
        var retryWait = SaveSettingsIfChanged(appState, ui.RealTime);
        if (retryWait > 0)
            ui.RequestUpdateIn(retryWait);
    }
}
