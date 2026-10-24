using ZGF.AppUtils;
using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

public sealed class IlSpyApp : ImmediateGuiApp<Workspace, UiState>
{
    private const string MonoFontFile = "JetBrainsMono-Regular.ttf";
    private const string ItalicFontFile = "Inter-Italic.ttf";

    private readonly List<string> _commandLineAssemblies;

    internal IlSpyApp(Workspace workspace, UiState screen, List<string> commandLineAssemblies) : base(workspace, screen)
    {
        _commandLineAssemblies = commandLineAssemblies;
    }

    public static void Launch(CommandLine commandLine)
    {
        var settings = AppSettings.Load(commandLine.SettingsPath);
        var workspace = new Workspace(settings);
        var screen = new UiState(workspace);
        var app = new IlSpyApp(workspace, screen, commandLine.AssemblyPaths);
        var windowConfig = settings.CreateWindowConfig();
        ImmediateGuiAppHost.Run(app, windowConfig, commandLine.RenderBackend);
    }

    protected override void OnStartup(ImmediateGuiAppHost host)
    {
        RegisterFonts(host);
        State.Assemblies.Changed += host.Invalidate;
        OpenStartupAssemblies();
        State.RestoreAnalyses();
    }

    private static void RegisterFonts(ImmediateGuiAppHost host)
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

    private void OpenStartupAssemblies()
    {
        State.RestoreActiveList();
        State.Open(_commandLineAssemblies);
    }

    protected override void OnShutdown(ImmediateGuiAppHost host)
    {
        State.Assemblies.Changed -= host.Invalidate;
        State.Settings.RememberWindow(host.WindowBounds);
        State.SaveSettings();
    }

    protected override void OnRender(Ui<UiState> ui, Workspace w)
    {
        ui.State.StartFrame();
        MainView.Render(ui, w);
        ui.State.FinishFrame();
        SaveSettingsIfChanged(ui, w);
    }

    /// <summary>Autosaves, and asks for a frame when a failed write may be tried again, as frames are
    /// drawn only on demand.</summary>
    private static void SaveSettingsIfChanged(Ui<UiState> ui, Workspace w)
    {
        var wait = w.SaveSettingsIfChanged(ui.RealTime);
        if (wait > 0)
            ui.RequestFrameIn(wait);
    }
}
