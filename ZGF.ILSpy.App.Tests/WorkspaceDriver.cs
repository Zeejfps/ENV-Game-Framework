using System.Collections.Concurrent;
using System.Diagnostics;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Testing;
using ZGF.Gui.Testing;
using ZGF.ILSpy.App.Tests.Fixtures;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

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

internal static class TempSettings
{
    public static AppSettings Settings(this TempDir temp) => AppSettings.Load(temp.PathOf("settings.json"));
}

/// <summary>A <see cref="Workspace"/> whose posted actions queue up until the test pumps them, so a
/// test decides what the user does before background results land. It speaks English unless its
/// settings name a locale.</summary>
internal sealed class WorkspaceDriver
{
    public const string FixturesNamespace = "ZGF.ILSpy.App.Tests.Fixtures";

    private readonly PostedActions _posted = PostedActions.OfThisTest();

    public WorkspaceDriver(AppSettings? settings = null)
    {
        settings ??= new AppSettings();
        settings.Locale ??= Locale.En;
        Workspace = new Workspace(settings);
        Workspace.RestoreLastSessionAnalyses();
    }

    /// <summary>A driver that has opened the fixture assembly.</summary>
    public static WorkspaceDriver Opened(AppSettings? settings = null)
    {
        var driver = new WorkspaceDriver(settings);
        driver.OpenFixtures();
        return driver;
    }

    public Workspace Workspace { get; }

    public static string FixturePath => typeof(Greeter).Assembly.Location;

    public void Pump()
    {
        _posted.Run();
        Workspace.Tree.Sync(Workspace.Assemblies);
        Workspace.SaveSettingsIfChanged();
    }

    /// <summary>Pumps until <paramref name="done"/>.</summary>
    public void RunUntil(Func<bool> done) => Poll.Until(done, Pump);

    /// <summary>Pumps until no background work the workspace knows of is left.</summary>
    public void Settle() => RunUntil(IsSettled);

    private bool IsSettled()
    {
        var w = Workspace;
        return !w.IsReloading && !w.IsResolving && !w.IsImporting && !w.IsExporting && !w.Search.IsBusy && w.Tabs.All(static t => !t.IsBusy) && _posted.IsEmpty;
    }

    /// <summary>Opens the fixture assembly and waits until its root shows.</summary>
    public LoadedAssembly OpenFixtures()
    {
        Workspace.Open(FixturePath);
        RunUntil(() => Workspace.Tabs.Active?.Node?.Kind == NodeKind.Assembly);
        Settle();
        return Workspace.Tabs.Active!.Node!.Assembly;
    }

    /// <summary>The listed node at the labels under the fixture assembly's root.</summary>
    public BrowseNode Node(params ReadOnlySpan<string> labels)
    {
        var fixtures = Workspace.Assemblies.Assemblies.Single(a => a.FilePath == FixturePath);
        return fixtures.Root.Child(labels);
    }

    public BrowseNode Fixture(string type) => Node(FixturesNamespace, type);

    public void Show(BrowseNode node, bool inNewTab = false)
    {
        Workspace.Show(node, inNewTab);
        Settle();
    }
}

/// <summary>Renders the whole app window frame by frame over a <see cref="WorkspaceDriver"/>, which
/// it pumps before each frame.</summary>
internal sealed class AppDriver
{
    public AppDriver(float width = 1000f, float height = 700f)
    {
        var app = new IlSpyApp(Workspace, new List<string>());
        Frames = UiFrames.Running(app, width: width, height: height);
        Frames.FrameTime = 1f / 60f;
        Frames.BeforeFrame = Driver.Pump;
    }

    /// <summary>An app that has opened the fixture assembly, before its first frame.</summary>
    public static AppDriver Opened(float width = 1000f, float height = 700f)
    {
        var app = new AppDriver(width, height);
        app.Driver.OpenFixtures();
        return app;
    }

    public WorkspaceDriver Driver { get; } = new();
    public Workspace Workspace => Driver.Workspace;
    public UiFrames Frames { get; }
    public UiTheme Theme => AppThemes.For(Workspace.Theme).Ui;

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

    /// <summary>Shows <paramref name="node"/> as <see cref="WorkspaceDriver.Show"/>, then runs <paramref name="frames"/> frames.</summary>
    public void Show(BrowseNode node, int frames = 0, bool inNewTab = false)
    {
        Driver.Show(node, inNewTab);
        Run(frames);
    }

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

    public UiFrames DoubleClick(string text) => Frames.DoubleClick(TextRect(text));
}
