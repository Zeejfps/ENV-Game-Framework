using System.Collections.Concurrent;
using System.Diagnostics;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Testing;
using ZGF.Gui.Testing;
using ZGF.ILSpy.App.Tests.Fixtures;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy.App.Tests;

/// <summary>A <see cref="Workspace"/> whose posted actions queue up until the test pumps them, so a
/// test decides what the user does before background results land.</summary>
internal sealed class WorkspaceDriver
{
    public const string FixturesNamespace = "ZGF.ILSpy.App.Tests.Fixtures";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly ConcurrentQueue<Action> _posted = new();

    public WorkspaceDriver(AppSettings? settings = null)
    {
        Workspace = new Workspace(settings ?? new AppSettings { Locale = Locale.En });
        Workspace.Attach(_posted.Enqueue);
    }

    public Workspace Workspace { get; }

    public static string FixturePath => typeof(Greeter).Assembly.Location;

    public void Post(Action action) => _posted.Enqueue(action);

    /// <summary>Runs what was posted so far, and what that posts.</summary>
    public void Pump()
    {
        while (_posted.TryDequeue(out var action))
            action();
        Workspace.Sync();
    }

    public void PumpUntil(Func<bool> done)
    {
        var clock = Stopwatch.StartNew();
        while (true)
        {
            Pump();
            if (done()) return;
            if (clock.Elapsed > Timeout) throw new TimeoutException("The workspace did not get there in time.");
            Thread.Sleep(1);
        }
    }

    /// <summary>Pumps until no background work the workspace knows of is left.</summary>
    public void Settle() => PumpUntil(() =>
    {
        var w = Workspace;
        return !w.IsReloading && !w.IsResolving && !w.IsImporting && !w.Search.IsBusy && w.Tabs.All(static t => !t.IsBusy) && _posted.IsEmpty;
    });

    /// <summary>Opens the fixture assembly and waits until its root shows.</summary>
    public LoadedAssembly OpenFixtures()
    {
        Workspace.Open(FixturePath);
        PumpUntil(() => Workspace.ActiveTab?.Node is { Kind: NodeKind.Assembly });
        Settle();
        return Workspace.ActiveTab!.Node!.Assembly;
    }

    /// <summary>The listed node at the labels under the fixture assembly's root.</summary>
    public BrowseNode Node(params string[] labels)
    {
        var node = Workspace.Assemblies.Assemblies.Single(a => a.FilePath == FixturePath).Root;
        foreach (var label in labels)
            node = node.Children.FirstOrDefault(c => c.Text == label)
                   ?? throw new InvalidOperationException($"'{node.Text}' has no child '{label}': {string.Join(", ", node.Children.Select(c => c.Text))}");
        return node;
    }

    public BrowseNode Fixture(string type) => Node(FixturesNamespace, type);

    public void Show(BrowseNode node, bool inNewTab = false)
    {
        Workspace.Show(node, inNewTab);
        Settle();
    }
}

/// <summary>Renders the whole app window frame by frame over a <see cref="WorkspaceDriver"/>.</summary>
internal sealed class AppDriver
{
    private const float Frame = 1f / 60f;

    public AppDriver(float width = 1000f, float height = 700f)
    {
        Frames = new UiFrames(width: width, height: height);
    }

    public WorkspaceDriver Driver { get; } = new();
    public Workspace Workspace => Driver.Workspace;
    public UiFrames Frames { get; }

    public void Run()
    {
        Driver.Pump();
        Frames.Run(Frame, ui => MainView.Render(ui, Workspace, Strings.For(Workspace.Locale)));
    }

    public void Run(int frames)
    {
        for (var i = 0; i < frames; i++)
            Run();
    }

    /// <summary>Where the text was drawn on the last frame, in window coordinates.</summary>
    public UiRect TextRect(string text) => TextRect(text, Frames.Canvas.Texts.FirstOrDefault(t => t.Inputs.Text == text));

    /// <summary>Where <paramref name="text"/> was last drawn: the one furthest into the window when
    /// the same text, such as an icon, is also drawn before it.</summary>
    public UiRect LastTextRect(string text) => TextRect(text, Frames.Canvas.Texts.LastOrDefault(t => t.Inputs.Text == text));

    private UiRect TextRect(string text, RecordedText? drawn)
    {
        if (drawn == null)
            throw new InvalidOperationException($"'{text}' was not drawn: {string.Join(", ", Frames.Canvas.Texts.Select(t => t.Inputs.Text))}");
        var p = drawn.Inputs.Position;
        return new UiRect(p.Left, Frames.Height - p.Top, p.Width, p.Height);
    }

    /// <summary>The texts drawn in the status bar, the window's bottom row.</summary>
    public IEnumerable<UiRect> StatusTexts()
    {
        var bottom = Frames.Height - AppThemes.For(Workspace.Theme).Ui.StatusBarHeight;
        return Frames.Canvas.Texts.Select(t => t.Inputs.Position).Where(p => Frames.Height - p.Top >= bottom - 0.5f)
            .Select(p => new UiRect(p.Left, Frames.Height - p.Top, p.Width, p.Height));
    }

    /// <summary>Where <paramref name="text"/> was drawn in the status bar on the last frame.</summary>
    public UiRect StatusText(string text)
    {
        var bottom = Frames.Height - AppThemes.For(Workspace.Theme).Ui.StatusBarHeight;
        var drawn = Frames.Canvas.Texts.LastOrDefault(t => t.Inputs.Text == text && Frames.Height - t.Inputs.Position.Top >= bottom - 0.5f)
                    ?? throw new InvalidOperationException($"'{text}' was not drawn in the status bar.");
        var p = drawn.Inputs.Position;
        return new UiRect(p.Left, Frames.Height - p.Top, p.Width, p.Height);
    }

    public void MoveTo(string text) => MoveTo(TextRect(text));

    public void Click(string text) => Click(TextRect(text));

    /// <summary>Clicks the last drawn <paramref name="text"/>: the one in the topmost open menu
    /// when the same text is also on the window below it.</summary>
    public void ClickMenuItem(string text) =>
        Click(TextRect(text, Frames.Canvas.Texts.LastOrDefault(t => t.Inputs.Text == text)));

    private void MoveTo(UiRect rect)
    {
        Frames.MovePointer(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        Run();
    }

    private void Click(UiRect rect)
    {
        MoveTo(rect);
        Frames.Press();
        Run();
        Frames.Release();
        Run();
    }

    public void DoubleClick(string text)
    {
        MoveTo(text);
        for (var i = 0; i < 2; i++)
        {
            Frames.Press();
            Run();
            Frames.Release();
            Run();
        }
    }
}
