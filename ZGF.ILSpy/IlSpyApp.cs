using System.Diagnostics;
using ZGF.AppUtils;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using static ZGF.ILSpy.Analysis;
using static ZGF.ILSpy.Appearance;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.AssemblyLists;
using static ZGF.ILSpy.CodeDisplay;
using static ZGF.ILSpy.CommandLines;
using static ZGF.ILSpy.IconTheming;
using static ZGF.ILSpy.Settings;
using static ZGF.ILSpy.ScreenSettings;
using static ZGF.ILSpy.Theming;

namespace ZGF.ILSpy;

/// <summary>The app: keeps the app state and the screen in step around each frame, and carries the
/// commands each queues for the other. Neither state sees the other; only the app and the views see both.</summary>
public sealed partial class IlSpyApp : ImmediateGuiApp<UiState>
{
    private const string MonoFontFile = "JetBrainsMono-Regular.ttf";
    private const string ItalicFontFile = "Inter-Italic.ttf";
    private const string AppIconFile = "app_icon.rgba";

    private readonly AppState _appState;
    private readonly CommandLine _startup;
    private readonly AppInstance? _instance;
    private readonly CodeFontFaces _codeFonts = new(LoadFont(MonoFonts.Code, MonoFontFile));

    internal IlSpyApp(
        AppState appState,
        UiState uiState,
        CommandLine startup,
        AppInstance? instance) : base(uiState)
    {
        _appState = appState;
        _startup = startup;
        _instance = instance;
    }

    protected override void OnStartup(ImmediateGuiAppHost<UiState> host)
    {
        ShowAppIcon(host);
        LoadAndRegisterFonts(host);
        RegisterChosenCodeFont(host);
        RegisterChosenIconFonts(host);
        OpenStartupAssemblies();
        WakeOnHandedCommandLines(host);
        StartRedrawingOnAssemblyChanges(host);
        RestoreAnalyses(_appState);
        WatchUserThemes(_appState);
        WatchUserIconThemes(_appState);
    }

    protected override void OnUpdate(Ui<UiState> ui)
    {
        FollowHandedCommandLines(ui);
        StartFrame(ui.State, _appState);
        MainView.Render(ui, _appState);
        FinishFrame(ui, _appState, _codeFonts);
    }

    protected override void OnShutdown(ImmediateGuiAppHost<UiState> host)
    {
        StopTakingCommandLines();
        StopRedrawingOnAssemblyChanges(host);
        StopWatchingUserThemes(_appState);
        StopWatchingUserIconThemes(_appState);
        SaveOnExit(host.WindowBounds);
    }

    /// <summary>Does what comes after drawing a frame: keeps what the screen changed, runs what it asked of
    /// the app state, asks for another frame when there is more to show, saves the changed settings, has
    /// the window draw at the chosen scale, code in the chosen font and icons in their fonts from the next
    /// frame, and opens the web pages and files asked for.</summary>
    private static void FinishFrame(Ui<UiState> ui, AppState appState, CodeFontFaces codeFonts)
    {
        KeepScreenChanges(ui.State, appState);
        RunAppCommandsAndRedraw(ui, appState);
        RequestUpdateIfViewCommandsPending(ui, appState);
        SaveChangedSettings(ui, appState);
        DrawAtChosenScale(ui);
        DrawCodeInChosenFont(ui, codeFonts);
        FollowIconTheme(ui);
        OpenAskedWebPages(ui);
        OpenAskedFiles(ui);
    }

    private static void OpenAskedWebPages(Ui<UiState> ui)
    {
        var pages = ui.State.WebPagesToOpen;
        while (pages.TryDequeue(out var url))
            ui.OpenUrl(url);
    }

    private static void OpenAskedFiles(Ui<UiState> ui)
    {
        var files = ui.State.FilesToOpen;
        while (files.TryDequeue(out var path))
            ui.OpenFile(path);
    }

    /// <summary>When the icons changed, lets go of the fonts they no longer draw in, registers those they
    /// are drawn in, and lets go of the pictures of the icons they replace.</summary>
    private static void FollowIconTheme(Ui<UiState> ui)
    {
        var shown = ui.State.ShownIcons;
        if (!shown.TakeChange(ui.State.Icons, ui.State.IconFonts))
            return;
        foreach (var family in shown.DroppedFamilies)
            ui.UnregisterFont(family);
        foreach (var font in ui.State.IconFonts)
            ui.RegisterFont(font);
        ui.ReleaseUndrawnIconImages();
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

    private static void ShowAppIcon(ImmediateGuiAppHost<UiState> host)
    {
        var packed = EmbeddedAssets.LoadBytes(typeof(IlSpyApp).Assembly, AppIconFile);
        host.SetIcon(WindowIcon.FromPacked(packed));
    }

    private void LoadAndRegisterFonts(ImmediateGuiAppHost<UiState> host)
    {
        host.RegisterFont(_codeFonts.Bundled);
        host.RegisterFont(LoadFont(TextFonts.Italic, ItalicFontFile));
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

    /// <summary>Hands the screen the chosen icon theme and registers its fonts before the first frame, so
    /// icons never show in another font first and the first frame finds them registered.</summary>
    private void RegisterChosenIconFonts(ImmediateGuiAppHost<UiState> host)
    {
        ApplyIconTheme(UiState, ResolveIconTheme(_appState));
        if (!UiState.ShownIcons.TakeChange(UiState.Icons, UiState.IconFonts))
            return;
        foreach (var font in UiState.IconFonts)
            host.RegisterFont(font);
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
        OpenCommandLineFiles(_appState, _startup);
    }

    /// <summary>Leaves the command lines of later runs to windows of their own once this one closes.</summary>
    private void StopTakingCommandLines()
    {
        if (_instance == null)
            return;
        _instance.StopListening();
    }

    /// <summary>Draws a frame whenever a later run hands this window a command line.</summary>
    private void WakeOnHandedCommandLines(ImmediateGuiAppHost<UiState> host)
    {
        if (_instance == null)
            return;
        _instance.WakeWith(host.Invalidate);
    }

    /// <summary>Does what the command lines later runs handed over ask, bringing the window to the
    /// front for each.</summary>
    private void FollowHandedCommandLines(Ui<UiState> ui)
    {
        if (_instance == null)
            return;
        while (_instance.TryTakeHanded(out var commandLine))
        {
            FollowHandedCommandLine(_appState, commandLine);
            ui.BringWindowToFront();
        }
    }

    internal void SaveOnExit(WindowBounds window)
    {
        _appState.Settings.RememberWindow(window);
        KeepViewSettings(_appState, TakeViewSettings(UiState));
        SaveSettings(_appState);
    }

    private static void SaveChangedSettings(Ui<UiState> ui, AppState appState)
    {
        switch (SaveSettingsIfChanged(appState, ui.RealTime))
        {
            case Saving.Unchanged:
            case Saving.Saved:
                break;
            case Saving.Waiting waiting:
                ui.RequestUpdateIn(waiting.RetryIn);
                break;
            case Saving.Failed failed:
                ui.RequestUpdateIn(failed.RetryIn);
                break;
            default:
                throw new UnreachableException();
        }
    }
}
