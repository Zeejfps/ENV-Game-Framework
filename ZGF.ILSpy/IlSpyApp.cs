using ZGF.AppUtils;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

public sealed class IlSpyApp : Gui<Workspace>
{
    private static readonly (string Family, string File)[] Fonts =
    [
        (MonoFonts.Regular, "JetBrainsMono-Regular.ttf"),
        (MonoFonts.Bold, "JetBrainsMono-Bold.ttf"),
        (MonoFonts.Italic, "JetBrainsMono-Italic.ttf"),
        (MonoFonts.BoldItalic, "JetBrainsMono-BoldItalic.ttf"),
        (LucideIcons.Family, LucideIcons.File),
    ];

    private readonly List<string> _commandLineAssemblies;

    private IlSpyApp(Workspace workspace, List<string> commandLineAssemblies) : base(workspace)
    {
        _commandLineAssemblies = commandLineAssemblies;
        foreach (var (family, file) in Fonts)
            RegisterFont(family, EmbeddedAssets.LoadBytes(typeof(IlSpyApp).Assembly, file));
    }

    public static void Launch(CommandLine commandLine)
    {
        var settings = AppSettings.Load(commandLine.SettingsPath);
        var workspace = new Workspace(settings);
        var app = new IlSpyApp(workspace, commandLine.AssemblyPaths);
        var windowConfig = settings.CreateWindowConfig();
        GuiHost.Run(app, windowConfig, commandLine.RenderBackend);
    }

    protected override void OnStartup(GuiHost host)
    {
        State.Assemblies.Changed += host.Invalidate;
        OpenStartupAssemblies();
    }

    private void OpenStartupAssemblies()
    {
        var paths = State.Settings.ListExistingAssemblies();
        paths.AddRange(_commandLineAssemblies);
        State.Open(paths);
    }

    protected override void OnShutdown(WindowBounds bounds)
    {
        State.Settings.RememberWindow(bounds);
        State.SaveSettings();
    }

    protected override void OnRender(Ui ui, Workspace w) => MainView.Render(ui, w, Strings.For(w.Locale));
}
