using ZGF.AppUtils;
using ZGF.Desktop;
using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

public sealed class IlSpyApp : ImmediateGuiApp<AppState, UiState>
{
    private const string MonoFontFile = "JetBrainsMono-Regular.ttf";
    private const string ItalicFontFile = "Inter-Italic.ttf";

    private readonly List<string> _startupAssemblies;

    internal IlSpyApp(
        AppState appState,
        UiState uiState,
        List<string> startupAssemblies) : base(appState, uiState)
    {
        _startupAssemblies = startupAssemblies;
    }

    protected override void OnStartup(ImmediateGuiAppHost host)
    {
        LoadAndRegisterFonts(host);
        OpenStartupAssemblies();
        StartRedrawingOnAssemblyChanges(host);
        State.RestoreAnalyses();
    }

    protected override void OnRender(Ui<UiState> ui, AppState w)
    {
        ui.State.StartFrame();
        MainView.Render(ui, w);
        ui.State.FinishFrame();
        RequestFrameIfViewCommandsPending(ui, w);
        SaveSettingsIfChanged(ui, w);
    }

    protected override void OnShutdown(ImmediateGuiAppHost host)
    {
        StopRedrawingOnAssemblyChanges(host);
        SaveOnExit(host.WindowBounds);
    }

    private static void LoadAndRegisterFonts(ImmediateGuiAppHost host)
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

    private void StartRedrawingOnAssemblyChanges(ImmediateGuiAppHost host)
    {
        State.Assemblies.Changed += host.Invalidate;
    }

    private void OpenStartupAssemblies()
    {
        State.RestoreActiveList();
        State.Open(_startupAssemblies);
    }

    private void StopRedrawingOnAssemblyChanges(ImmediateGuiAppHost host)
    {
        State.Assemblies.Changed -= host.Invalidate;
    }

    internal void SaveOnExit(WindowBounds window)
    {
        State.Settings.RememberWindow(window);
        UiState.HandOverViewSettings();
        State.SaveSettings();
    }
    
    private static void RequestFrameIfViewCommandsPending(Ui<UiState> ui, AppState w)
    {
        if (w.ViewCommands.Count == 0)
            return;
        ui.RequestFrame();
    }
    
    private static void SaveSettingsIfChanged(Ui<UiState> ui, AppState w)
    {
        var retryWait = w.SaveSettingsIfChanged(ui.RealTime);
        if (retryWait > 0)
            ui.RequestFrameIn(retryWait);
    }
}
