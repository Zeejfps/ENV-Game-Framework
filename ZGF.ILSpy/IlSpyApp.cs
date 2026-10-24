using ZGF.AppUtils;
using ZGF.Desktop;
using ZGF.Gui.Immediate;

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
        _appState.RestoreAnalyses();
    }

    protected override void OnUpdate(Ui<UiState> ui)
    {
        RenderFrame(ui, _appState);
        RequestUpdateIfViewCommandsPending(ui, _appState);
        SaveSettingsIfChanged(ui, _appState);
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
        _appState.RestoreActiveList();
        _appState.Open(_startupAssemblies);
    }

    private void StopRedrawingOnAssemblyChanges(ImmediateGuiAppHost<UiState> host)
    {
        _appState.Assemblies.Changed -= host.Invalidate;
    }

    internal void SaveOnExit(WindowBounds window)
    {
        _appState.Settings.RememberWindow(window);
        UiState.HandOverViewSettings();
        _appState.SaveSettings();
    }
    
    private static void RequestUpdateIfViewCommandsPending(Ui<UiState> ui, AppState w)
    {
        if (w.ViewCommands.Count == 0)
            return;
        ui.RequestUpdate();
    }
    
    private static void SaveSettingsIfChanged(Ui<UiState> ui, AppState w)
    {
        var retryWait = w.SaveSettingsIfChanged(ui.RealTime);
        if (retryWait > 0)
            ui.RequestUpdateIn(retryWait);
    }
}
