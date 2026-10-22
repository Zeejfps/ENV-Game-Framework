using ZGF.AppUtils;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
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

    private IlSpyApp(Workspace workspace) : base(workspace)
    {
        foreach (var (family, file) in Fonts)
            RegisterFont(family, EmbeddedAssets.LoadBytes(typeof(IlSpyApp).Assembly, file));
        workspace.Assemblies.Changed += Invalidate;
    }

    public static void Launch(CommandLine commandLine)
    {
        var app = Create(commandLine.SettingsPath);
        app.OpenOnStartup(commandLine.AssemblyPaths);
        var windowConfig = app.State.Settings.CreateWindowConfig();
        app.Run(windowConfig, commandLine.RenderBackend);
        app.SaveOnClose();
    }

    private static IlSpyApp Create(string settingsPath)
    {
        var settings = AppSettings.Load(settingsPath);
        var workspace = new Workspace(settings);
        return new IlSpyApp(workspace);
    }

    private void OpenOnStartup(List<string> commandLinePaths)
    {
        var paths = State.Settings.ListExistingAssemblies();
        paths.AddRange(commandLinePaths);
        Post(() => State.Open(paths));
    }

    private void SaveOnClose()
    {
        State.Settings.RememberWindow(WindowBounds);
        State.SaveSettings();
    }

    protected override void Render(Ui ui, Workspace w) => MainView.Render(ui, w, Strings.For(w.Locale));
}
