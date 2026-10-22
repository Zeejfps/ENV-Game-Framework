using ZGF.AppUtils;
using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

public sealed class IlSpyApp : ImmediateGuiApp<Workspace>
{
    private const string MonoFontFile = "JetBrainsMono-Regular.ttf";

    private readonly List<string> _commandLineAssemblies;

    private IlSpyApp(Workspace workspace, List<string> commandLineAssemblies) : base(workspace)
    {
        _commandLineAssemblies = commandLineAssemblies;
    }

    public static void Launch(CommandLine commandLine)
    {
        var settings = AppSettings.Load(commandLine.SettingsPath);
        var workspace = new Workspace(settings);
        var app = new IlSpyApp(workspace, commandLine.AssemblyPaths);
        var windowConfig = settings.CreateWindowConfig();
        ImmediateGuiAppHost.Run(app, windowConfig, commandLine.RenderBackend);
    }

    protected override void OnStartup(ImmediateGuiAppHost host)
    {
        RegisterFonts(host);
        State.Assemblies.Changed += host.Invalidate;
        OpenStartupAssemblies();
    }

    private static void RegisterFonts(ImmediateGuiAppHost host)
    {
        host.RegisterFont(LoadFont(MonoFonts.Regular, MonoFontFile));
        host.RegisterFont(LoadFont(LucideIcons.Family, LucideIcons.File));
    }

    private static FontFace LoadFont(string family, string file)
    {
        var data = EmbeddedAssets.LoadBytes(typeof(IlSpyApp).Assembly, file);
        return new FontFace(family, data);
    }

    private void OpenStartupAssemblies()
    {
        var paths = State.Settings.ListExistingAssemblies();
        paths.AddRange(_commandLineAssemblies);
        State.Open(paths);
    }

    protected override void OnShutdown(ImmediateGuiAppHost host)
    {
        State.Assemblies.Changed -= host.Invalidate;
        State.Settings.RememberWindow(host.WindowBounds);
        State.SaveSettings();
    }

    protected override void OnRender(Ui ui, Workspace w)
    {
        MainView.Render(ui, w);
    }
}
