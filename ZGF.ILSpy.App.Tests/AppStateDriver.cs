using System.Collections.Concurrent;
using System.Diagnostics;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Testing;
using ZGF.Gui.Testing;
using ZGF.ILSpy.App.Tests.Fixtures;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Analysis;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.Navigation;
using static ZGF.ILSpy.Settings;

namespace ZGF.ILSpy.App.Tests;

/// <summary>Actions posted from other threads, queued until the test runs them. A test's drivers
/// share one, which is also where <c>await</c> on the test's thread continues.</summary>
internal sealed class PostedActions
{
    [ThreadStatic] private static PostedActions? _ofThisTest;

    private readonly ConcurrentQueue<Action> _queue = new();
    private readonly UiSynchronizationContext _context;

    private PostedActions()
    {
        _context = new UiSynchronizationContext(Post);
    }

    public bool IsEmpty => _queue.IsEmpty;

    /// <summary>The queue of the test running on this thread. xUnit gives every test a context of
    /// its own, so a test that has not made one yet gets a new queue.</summary>
    public static PostedActions OfThisTest()
    {
        var existing = _ofThisTest;
        if (existing != null && SynchronizationContext.Current == existing._context)
            return existing;
        var posted = new PostedActions();
        SynchronizationContext.SetSynchronizationContext(posted._context);
        _ofThisTest = posted;
        return posted;
    }

    public void Post(Action action) => _queue.Enqueue(action);

    /// <summary>Runs what was posted so far, and what that posts.</summary>
    public void Run()
    {
        while (_queue.TryDequeue(out var action))
            action();
    }
}

internal static class Poll
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    /// <summary>Takes <paramref name="step"/> until <paramref name="done"/>, at least once.</summary>
    public static void Until(Func<bool> done, Action step)
    {
        var clock = Stopwatch.StartNew();
        while (true)
        {
            step();
            if (done()) return;
            if (clock.Elapsed > Timeout) throw new TimeoutException("It did not get there in time.");
            Thread.Sleep(1);
        }
    }
}

internal static class FoundAnalyses
{
    /// <summary>The analyzer's roots whose symbols were found.</summary>
    public static List<AnalyzerSymbolNode> Found(this AnalyzerTree analyzer)
    {
        return analyzer.Roots
            .OfType<AnalyzerSymbolNode>()
            .ToList();
    }
}

/// <summary>The icons of the built-in reference icon theme, which the tests' app draws with.</summary>
internal static class BuiltInGlyphs
{
    public static AppIcons Icons { get; } = BuiltInIconThemes.Reference.Icons;

    /// <summary>The text <paramref name="glyph"/>, a font glyph, is drawn as.</summary>
    public static string Text(IconGlyph glyph) => glyph.Text!;
}

internal static class TempSettings
{
    public static AppSettings Settings(this TempDir temp) => AppSettings.Load(temp.PathOf("settings.json"));
}

internal static class Screens
{
    /// <summary>A screen over an app state with nothing open, for drawing a widget alone.</summary>
    public static UiState Blank() => IlSpyApp.CreateUiState(new AppState(new AppSettings { Locale = Locale.En }));
}

/// <summary>A <see cref="AppState"/> and, unless made without one, the screen over it, whose posted
/// actions queue up until the test pumps them, so a test decides what the user does before background
/// results land. A frame's steps around drawing run only when the test pumps or asks for
/// <see cref="NextFrame"/>; reading the driver changes nothing. It speaks English unless its settings
/// name a locale.</summary>
internal sealed class AppStateDriver
{
    public const string FixturesNamespace = "ZGF.ILSpy.App.Tests.Fixtures";

    private readonly PostedActions _posted = PostedActions.OfThisTest();
    private readonly UiState? _ui;

    public AppStateDriver(AppSettings? settings = null) : this(settings, withScreen: true)
    {
    }

    private AppStateDriver(AppSettings? settings, bool withScreen)
    {
        settings ??= new AppSettings();
        settings.Locale ??= Locale.En;
        AppState = new AppState(settings);
        if (withScreen)
            _ui = IlSpyApp.CreateUiState(AppState);
        RestoreAnalyses(AppState);
    }

    /// <summary>A driver that has opened the fixture assembly.</summary>
    public static AppStateDriver Opened(AppSettings? settings = null)
    {
        var driver = new AppStateDriver(settings);
        driver.OpenFixtures();
        return driver;
    }

    /// <summary>A driver with an app state and no screen, which shows that the features need none.</summary>
    public static AppStateDriver WithoutScreen(AppSettings? settings = null) => new(settings, withScreen: false);

    /// <summary>A driver with no screen that has opened the fixture assembly.</summary>
    public static AppStateDriver OpenedWithoutScreen()
    {
        var driver = WithoutScreen();
        driver.OpenFixtures();
        return driver;
    }

    public AppState AppState { get; }

    /// <summary>The screen over <see cref="AppState"/>; a driver made without one has none to give.</summary>
    public UiState Ui => _ui ?? throw new InvalidOperationException("This driver has no screen.");

    /// <summary>The time the driver's autosave goes by, in seconds.</summary>
    public double Now { get; set; }

    public static string FixturePath => typeof(Greeter).Assembly.Location;

    /// <summary>Runs what was posted, then what a frame does around drawing when there is a screen,
    /// then the autosave.</summary>
    public void Pump()
    {
        _posted.Run();
        if (_ui != null)
            NextFrame();
        SaveSettingsIfChanged(AppState, Now);
    }

    /// <summary>Does what a frame does before and after it draws: the screen follows the trees and the
    /// tabs and runs the commands the features queued for it, then tells the tabs where their views stand and
    /// hands over what the settings keep of it, and the app runs what the screen asked of the app state.</summary>
    public void NextFrame()
    {
        IlSpyApp.StartFrame(Ui, AppState);
        IlSpyApp.KeepScreenChanges(Ui, AppState);
        IlSpyApp.RunAppCommands(Ui, AppState);
    }

    /// <summary>Pumps until <paramref name="done"/>.</summary>
    public void RunUntil(Func<bool> done) => Poll.Until(done, Pump);

    /// <summary>Has what is picked and followed shown in the active tab, as the display option does.</summary>
    public void TurnOffPreviewTabs()
    {
        if (_ui != null)
            _ui.Display = _ui.Display with { PreviewTabs = false };
        Navigation.UsePreviewTabs(AppState, false);
    }

    /// <summary>Pumps until no background work the app state knows of is left.</summary>
    public void Settle() => RunUntil(IsSettled);

    private bool IsSettled()
    {
        var appState = AppState;
        return !appState.IsReloading && !appState.IsReopening && !appState.IsFindingDocuments && !appState.IsFindingAnalyses &&!appState.IsResolving && !appState.IsImporting && !appState.IsExporting && !appState.IsDiagramming && !appState.IsComparing && !IsSearching() && appState.Tabs.All(static t => !t.IsBusy) && _posted.IsEmpty;
    }

    private bool IsSearching()
    {
        if (_ui == null)
            return false;
        return _ui.Search.IsBusy;
    }

    /// <summary>The commands the features queued for the view since the last frame ran them, oldest first, taken
    /// off the queue as the screen would.</summary>
    public List<ViewCommand> TakeViewCommands()
    {
        var commands = AppState.ViewCommands.ToList();
        AppState.ViewCommands.Clear();
        return commands;
    }

    /// <summary>Opens the fixture assembly and waits until its root shows.</summary>
    public LoadedAssembly OpenFixtures()
    {
        OpenAssembly(AppState, FixturePath);
        RunUntil(() => AppState.Tabs.Active?.Node?.Kind == NodeKind.Assembly);
        Settle();
        return AppState.Tabs.Active!.Node!.Assembly;
    }

    /// <summary>The listed node at the labels under the fixture assembly's root.</summary>
    public BrowseNode Node(params ReadOnlySpan<string> labels)
    {
        var fixtures = AppState.Assemblies.Assemblies.Single(a => a.FilePath == FixturePath);
        return fixtures.Root.Child(labels);
    }

    public BrowseNode Fixture(string type) => Node(FixturesNamespace, type);

    public void Show(BrowseNode node, bool inNewTab = false)
    {
        ShowNode(AppState, node, inNewTab);
        Settle();
    }

    /// <summary>Analyzes <paramref name="node"/> as the views do.</summary>
    public void Analyze(BrowseNode node) => AnalyzerPanelView.Analyze(Ui, AppState, node);

    /// <summary>Analyzes what <paramref name="reference"/> points at as the views do.</summary>
    public void Analyze(CodeReference reference) => AnalyzerPanelView.Analyze(Ui, AppState, reference);

    /// <summary>Opens the export dialog on the open assemblies, with <paramref name="picked"/> ticked.</summary>
    public void OpenExportSetup(LoadedAssembly? picked) => Ui.ExportSetup.Open(AppState.Assemblies.Assemblies, picked);
}

/// <summary>Renders the whole app window frame by frame over a <see cref="AppStateDriver"/>, which
/// it pumps before each frame.</summary>
internal sealed class AppDriver
{
    public AppDriver(float width = 1000f, float height = 700f, AppSettings? settings = null, AppInstance? instance = null)
    {
        Driver = new AppStateDriver(settings);
        App = new IlSpyApp(AppState, Ui, CommandLine.Parse([]), instance);
        Frames = UiFrames.Running(App, width: width, height: height);
        Frames.FrameTime = 1f / 60f;
        Frames.BeforeFrame = PumpAtFrameTime;
    }

    private void PumpAtFrameTime()
    {
        Driver.Now = Frames.RealTime;
        Driver.Pump();
    }

    /// <summary>An app that has opened the fixture assembly, before its first frame.</summary>
    public static AppDriver Opened(float width = 1000f, float height = 700f)
    {
        var app = new AppDriver(width, height);
        app.Driver.OpenFixtures();
        return app;
    }

    public AppStateDriver Driver { get; }
    public AppState AppState => Driver.AppState;

    /// <summary>The screen the app draws.</summary>
    public UiState Ui => Driver.Ui;
    public IlSpyApp App { get; }
    public UiFrames Frames { get; }
    public UiTheme Theme => Theming.ResolveTheme(AppState).Ui;

    public BrowseNode Node(params ReadOnlySpan<string> labels) => Driver.Node(labels);

    public BrowseNode Fixture(string type) => Driver.Fixture(type);

    public void Run(int frames = 1) => Frames.Frame(frames);

    /// <summary>Runs frames until <paramref name="done"/>.</summary>
    public void RunUntil(Func<bool> done) => Poll.Until(done, () => Frames.Frame());

    /// <summary>Waits out the background work, then runs <paramref name="frames"/> frames.</summary>
    public void Settle(int frames = 0)
    {
        Driver.Settle();
        Run(frames);
    }

    /// <summary>Shows <paramref name="node"/> as <see cref="AppStateDriver.Show"/>, then runs <paramref name="frames"/> frames.</summary>
    public void Show(BrowseNode node, int frames = 0, bool inNewTab = false)
    {
        Driver.Show(node, inNewTab);
        Run(frames);
    }

    public void Analyze(BrowseNode node) => Driver.Analyze(node);

    public void OpenExportSetup(LoadedAssembly? picked) => Driver.OpenExportSetup(picked);

    /// <summary>Where <paramref name="icon"/> was drawn on the last frame, in window coordinates.</summary>
    public UiRect TextRect(IconGlyph icon) => TextRect(BuiltInGlyphs.Text(icon));

    /// <summary>Where <paramref name="icon"/> was last drawn, as <see cref="LastTextRect(string)"/>.</summary>
    public UiRect LastTextRect(IconGlyph icon) => LastTextRect(BuiltInGlyphs.Text(icon));

    public UiFrames Click(IconGlyph icon) => Click(BuiltInGlyphs.Text(icon));

    /// <summary>Clicks where <paramref name="icon"/> was last drawn, as <see cref="ClickLast(string)"/>.</summary>
    public UiFrames ClickLast(IconGlyph icon) => ClickLast(BuiltInGlyphs.Text(icon));

    /// <summary>Where the text was drawn on the last frame, in window coordinates.</summary>
    public UiRect TextRect(string text) => Drawn(text, Frames.Canvas.Texts.FirstOrDefault(t => t.Inputs.Text == text));

    /// <summary>Where <paramref name="text"/> was last drawn: the one furthest into the window when
    /// the same text, such as an icon, is also drawn before it.</summary>
    public UiRect LastTextRect(string text) => Drawn(text, Frames.Canvas.Texts.LastOrDefault(t => t.Inputs.Text == text));

    private UiRect Drawn(string text, RecordedText? drawn)
    {
        if (drawn == null)
            throw new InvalidOperationException($"'{text}' was not drawn: {string.Join(", ", Frames.Canvas.Texts.Select(t => t.Inputs.Text))}");
        return Frames.ToUi(drawn.Inputs.Position);
    }

    /// <summary>Where the tab titled <paramref name="title"/> was drawn on the last frame; the first of
    /// those drawn when several tabs are.</summary>
    public UiRect TabTitleRect(string title) => ListTabTitleRects(title)[0];

    /// <summary>Where the tab titled <paramref name="title"/> was drawn on the last frame; the last of
    /// those drawn when several tabs are.</summary>
    public UiRect LastTabTitleRect(string title)
    {
        var rects = ListTabTitleRects(title);
        return rects[rects.Count - 1];
    }

    /// <summary>The texts reading <paramref name="title"/> drawn highest, in the order drawn: tab titles,
    /// over the omnibar and the tree that name the same node.</summary>
    private List<UiRect> ListTabTitleRects(string title)
    {
        var rects = Frames.TextRects()
            .Where(t => t.Text == title)
            .Select(t => t.Rect)
            .ToList();
        if (rects.Count == 0)
            throw new InvalidOperationException($"'{title}' was not drawn.");
        var top = rects.Min(r => r.Y);
        return rects
            .Where(r => r.Y < top + 0.5f)
            .ToList();
    }

    /// <summary>The texts drawn in the status bar, the window's bottom row.</summary>
    public IEnumerable<UiRect> StatusTexts()
    {
        var top = StatusBarTop;
        return Frames.TextRects()
            .Where(t => t.Rect.Y >= top)
            .Select(t => t.Rect);
    }

    /// <summary>Where <paramref name="text"/> was drawn in the status bar on the last frame.</summary>
    public UiRect StatusText(string text)
    {
        var top = StatusBarTop;
        var (drawn, rect) = Frames.TextRects().LastOrDefault(t => t.Text == text && t.Rect.Y >= top);
        if (drawn == null)
            throw new InvalidOperationException($"'{text}' was not drawn in the status bar.");
        return rect;
    }

    private float StatusBarTop => Frames.Height - Theme.StatusBarHeight - 0.5f;

    public UiFrames MoveTo(string text) => Frames.MovePointer(TextRect(text)).Frame();

    public UiFrames Click(string text) => Frames.Click(TextRect(text));

    /// <summary>Clicks the first text drawn starting with <paramref name="prefix"/>, as a label cut
    /// short with an ellipsis is.</summary>
    public UiFrames ClickStartingWith(string prefix)
    {
        var drawn = Frames.Canvas.Texts.FirstOrDefault(t => t.Inputs.Text.StartsWith(prefix, StringComparison.Ordinal));
        return Frames.Click(Drawn(prefix, drawn));
    }

    /// <summary>Clicks the last drawn <paramref name="text"/>: the one in the topmost open menu
    /// when the same text is also on the window below it.</summary>
    public UiFrames ClickMenuItem(string text) => Frames.Click(LastTextRect(text));

    /// <summary>Right-clicks the tree row drawn as <paramref name="row"/> and picks <paramref name="item"/>
    /// from its menu.</summary>
    public void PickFromTreeMenu(string row, string item)
    {
        var at = TextRect(row);
        Frames.Click(at.X + 2f, at.Y + at.Height / 2, PointerButton.Right).Frame();
        ClickMenuItem(item).Frame();
    }

    /// <summary>Clicks where <paramref name="text"/> was last drawn, such as an icon that the assemblies
    /// panel's toolbar also draws.</summary>
    public UiFrames ClickLast(string text) => Frames.Click(LastTextRect(text));

    public UiFrames DoubleClick(string text) => Frames.DoubleClick(TextRect(text));
}
