using ZGF.AppUtils;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using static ZGF.ILSpy.Analysis;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.AssemblyLists;
using static ZGF.ILSpy.CodeDisplay;
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
    private readonly CodeFontFaces _codeFonts = new(LoadFont(MonoFonts.Code, MonoFontFile));

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
        RegisterChosenCodeFont(host);
        OpenStartupAssemblies();
        StartRedrawingOnAssemblyChanges(host);
        RestoreAnalyses(_appState);
    }

    protected override void OnUpdate(Ui<UiState> ui)
    {
        StartFrame(ui.State, _appState);
        MainView.Render(ui, _appState);
        FinishFrame(ui, _appState, _codeFonts);
    }

    protected override void OnShutdown(ImmediateGuiAppHost<UiState> host)
    {
        StopRedrawingOnAssemblyChanges(host);
        SaveOnExit(host.WindowBounds);
    }

    /// <summary>Does what comes after drawing a frame: keeps what the screen changed, runs what it asked of
    /// the app state, asks for another frame when there is more to show, saves the changed settings, has
    /// the window draw at the chosen scale and code in the chosen font from the next frame, and opens the
    /// web pages asked for.</summary>
    private static void FinishFrame(Ui<UiState> ui, AppState appState, CodeFontFaces codeFonts)
    {
        KeepScreenChanges(ui.State, appState);
        RunAppCommandsAndRedraw(ui, appState);
        RequestUpdateIfViewCommandsPending(ui, appState);
        SaveChangedSettings(ui, appState);
        DrawAtChosenScale(ui);
        DrawCodeInChosenFont(ui, codeFonts);
        OpenAskedWebPages(ui);
    }

    private static void OpenAskedWebPages(Ui<UiState> ui)
    {
        var pages = ui.State.WebPagesToOpen;
        while (pages.TryDequeue(out var url))
            ui.OpenUrl(url);
    }

    private static void DrawAtChosenScale(Ui<UiState> ui)
    {
        ui.Scale = ui.State.Display.UiScale;
    }

    private static void DrawCodeInChosenFont(Ui<UiState> ui, CodeFontFaces codeFonts)
    {
        var face = TakeCodeFontFace(ui.State, codeFonts);
        if (face == null)
            return;
        ui.RegisterFont(face);
    }

    /// <summary>The face to draw code with when the chosen font changed since code was last drawn, else
    /// null. A chosen font whose file cannot be read is given up for the bundled one.</summary>
    private static FontFace? TakeCodeFontFace(UiState uiState, CodeFontFaces codeFonts)
    {
        var chosen = uiState.Display.Font;
        if (!uiState.ShownCodeFont.TakeChange(chosen))
            return null;
        var face = codeFonts.FaceOf(chosen);
        if (face != null)
            return face;
        SetCodeFont(uiState, null);
        uiState.ShownCodeFont.TakeChange(null);
        return codeFonts.Bundled;
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

    private void LoadAndRegisterFonts(ImmediateGuiAppHost<UiState> host)
    {
        host.RegisterFont(_codeFonts.Bundled);
        host.RegisterFont(LoadFont(TextFonts.Italic, ItalicFontFile));
        host.RegisterFont(LoadFont(LucideIcons.Family, LucideIcons.File));
    }

    /// <summary>Registers the chosen code font before the first frame, so code never shows in the
    /// bundled one first.</summary>
    private void RegisterChosenCodeFont(ImmediateGuiAppHost<UiState> host)
    {
        var face = TakeCodeFontFace(UiState, _codeFonts);
        if (face == null)
            return;
        host.RegisterFont(face);
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
