using ZGF.AppUtils;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using static ZGF.ILSpy.Analysis;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.AssemblyLists;
using static ZGF.ILSpy.Settings;
using static ZGF.ILSpy.ScreenSettings;

namespace ZGF.ILSpy;

/// <summary>The app: keeps the app state and the screen in step around each frame, and carries the
/// commands each queues for the other. Neither state sees the other; only the app and the views see both.</summary>
public sealed partial class IlSpyApp : ImmediateGuiApp<UiState>
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
        StartFrame(ui.State, _appState);
        MainView.Render(ui, _appState);
        FinishFrame(ui, _appState);
    }

    protected override void OnShutdown(ImmediateGuiAppHost<UiState> host)
    {
        StopRedrawingOnAssemblyChanges(host);
        SaveOnExit(host.WindowBounds);
    }

    /// <summary>Does what comes after drawing a frame: keeps what the screen changed, runs what it asked of
    /// the app state, asks for another frame when there is more to show, and saves the changed settings.</summary>
    private static void FinishFrame(Ui<UiState> ui, AppState appState)
    {
        KeepScreenChanges(ui.State, appState);
        RunAppCommandsAndRedraw(ui, appState);
        RequestUpdateIfViewCommandsPending(ui, appState);
        SaveChangedSettings(ui, appState);
    }

    /// <summary>Runs what the screen asked of the app state while it drew, and draws again to show it.</summary>
    private static void RunAppCommandsAndRedraw(Ui<UiState> ui, AppState appState)
    {
        if (ui.State.AppCommands.Count == 0)
            return;
        RunAppCommands(ui.State, appState);
        ui.RequestUpdate();
    }

    private static void RequestUpdateIfViewCommandsPending(Ui<UiState> ui, AppState appState)
    {
        if (appState.ViewCommands.Count == 0)
            return;
        ui.RequestUpdate();
    }

    private static void LoadAndRegisterFonts(ImmediateGuiAppHost<UiState> host)
    {
        host.RegisterFont(LoadFont(MonoFonts.Regular, MonoFontFile));
        host.RegisterFont(LoadFont(TextFonts.Italic, ItalicFontFile));
        host.RegisterFont(LoadFont(LucideIcons.Family, LucideIcons.File));
    }

    private void StopRedrawingOnAssemblyChanges(ImmediateGuiAppHost<UiState> host)
    {
        _appState.Assemblies.Changed -= host.Invalidate;
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

    internal void SaveOnExit(WindowBounds window)
    {
        _appState.Settings.RememberWindow(window);
        KeepViewSettings(_appState, TakeViewSettings(UiState));
        SaveSettings(_appState);
    }

    private static void SaveChangedSettings(Ui<UiState> ui, AppState appState)
    {
        var retryWait = SaveSettingsIfChanged(appState, ui.RealTime);
        if (retryWait > 0)
            ui.RequestUpdateIn(retryWait);
    }
}
