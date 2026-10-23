using ZGF.AppUtils;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

public sealed class IlSpyApp : ImmediateGuiApp<Workspace, UiState>
{
    private const string MonoFontFile = "JetBrainsMono-Regular.ttf";
    private const string ItalicFontFile = "Inter-Italic.ttf";

    private readonly List<string> _commandLineAssemblies;

    internal IlSpyApp(Workspace workspace, List<string> commandLineAssemblies) : base(workspace, new UiState())
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
        State.RestoreLastSessionAnalyses();
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
        ApplyThemeAndLanguage(ui.State, w);
        w.Tree.Sync(w.Assemblies);
        MainView.Render(ui, w);
        w.SaveSettingsIfChanged();
    }

    private static void ApplyThemeAndLanguage(UiState state, Workspace w)
    {
        var theme = AppThemes.For(w.Theme);
        state.Theme = theme.Ui;
        state.CodeViewPalette = theme.Code;
        state.NodeIconPalette = theme.Icons;
        state.Strings = Strings.For(w.Locale);
    }
}
