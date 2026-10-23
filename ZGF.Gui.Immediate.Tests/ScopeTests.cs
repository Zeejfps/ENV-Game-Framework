using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class ScopeTests
{
    private sealed class Service(string name)
    {
        public string Name { get; } = name;
    }

    private sealed class Probe(Ui ui)
    {
        public Ui Ui { get; } = ui;
        public List<string> Seen { get; } = new();
    }

    private static void RecordOpenService(object state)
    {
        var probe = (Probe)state;
        var service = probe.Ui.FindOpenGroup<Service>();
        probe.Seen.Add(service != null ? service.Name : "none");
    }

    private static void EndNothing(object state) { }

    [Fact]
    public void Disposing_a_group_ends_every_scope_opened_inside_it_and_then_runs_its_end()
    {
        var seen = new List<string>();
        var frames = new UiFrames(body: ui =>
        {
            var probe = new Probe(ui);
            var outside = ui.GetId("key");
            using (ui.Group(probe, RecordOpenService))
            {
                ui.Group(new Service("inner"), EndNothing);
                ui.Id("nested");
                ui.Box(new Box { Width = 10f, Height = 10f });
                seen.Add(ui.FindOpenGroup<Service>()!.Name);
            }
            seen.AddRange(probe.Seen);
            seen.Add(ui.GetId("key") == outside ? "same id" : "other id");
        });

        frames.Frame();

        Assert.Equal(["inner", "none", "same id"], seen);
    }

    [Fact]
    public void Disposing_an_outer_scope_ends_the_scopes_left_open_inside_it()
    {
        var frames = new UiFrames(body: ui =>
        {
            using (ui.Column(new Box { Width = 100f }))
            {
                ui.Row(new Box { Height = 20f });
                ui.Group(new Service("left open"), EndNothing);
            }
            Assert.Null(ui.FindOpenGroup<Service>());
        });

        frames.Frame();
    }

    [Fact]
    public void A_default_scope_is_not_open_and_ends_nothing()
    {
        string? seen = null;
        var frames = new UiFrames(body: ui =>
        {
            using (ui.Group(new Service("kept"), EndNothing))
            {
                var closed = default(Ui.Scope);
                Assert.False(closed.IsOpen);
                closed.Dispose();
                seen = ui.FindOpenGroup<Service>()?.Name;
            }
        });

        frames.Frame();

        Assert.Equal("kept", seen);
    }

    [Fact]
    public void A_scope_left_open_fails_the_frame_by_its_kind_and_the_next_frame_starts_clean()
    {
        var frames = new UiFrames();

        var error = Assert.Throws<InvalidOperationException>(() => frames.Run(0f, ui => { ui.Id("leaked"); }));
        frames.Run(0f, ui => { using (ui.Box()) { } });

        Assert.Contains("Id", error.Message);
    }

    [Fact]
    public void A_closed_widget_returns_a_scope_that_is_not_open()
    {
        var open = false;
        var frames = new UiFrames();

        var dialogOpen = frames.Run(0f, ui =>
        {
            using var dialog = ui.Dialog("dialog", "Title", ref open);
            return dialog.IsOpen;
        });
        var menuOpen = frames.Run(0f, ui =>
        {
            using var menu = ui.ContextMenu("menu");
            return menu.IsOpen;
        });

        Assert.False(dialogOpen);
        Assert.False(menuOpen);
    }

    [Fact]
    public void A_menu_bar_closes_its_menu_once_the_open_title_stops_being_recorded()
    {
        var showFile = true;
        var frames = new UiFrames(width: 400f, height: 300f, body: ui =>
        {
            using (ui.MenuBar("bar"))
            {
                if (showFile)
                {
                    using var file = ui.Menu("File");
                    if (file.IsOpen)
                        ui.MenuItem("Open");
                }
                using var edit = ui.Menu("Edit");
                if (edit.IsOpen)
                    ui.MenuItem("Undo");
            }
        });
        frames.Frame();
        frames.ClickOn(ui => ui.GetId("File")).Frame(2);
        Assert.True(frames.Drew("Open"));

        showFile = false;
        frames.Frame();
        showFile = true;
        frames.Frame();

        Assert.False(frames.Drew("Open"));
    }

    [Fact]
    public void A_group_allocates_no_more_per_frame_than_the_scopes_it_ends()
    {
        var frames = new UiFrames();

        Assert.Equal(AllocatedPerRun(frames, Nested), AllocatedPerRun(frames, Grouped));
    }

    private static readonly object Token = new();

    private static void Grouped(Ui ui)
    {
        using (ui.Group(Token, static _ => { }))
        {
            ui.Group(Token, static _ => { });
            ui.Id("nested");
            ui.Box(new Box { Width = 10f, Height = 10f });
        }
    }

    private static void Nested(Ui ui)
    {
        using (ui.Group(Token, static _ => { }))
        using (ui.Id("nested"))
        using (ui.Box(new Box { Width = 10f, Height = 10f })) { }
    }

    private static long AllocatedPerRun(UiFrames frames, Action<Ui> body)
    {
        for (var i = 0; i < 4; i++)
            frames.Run(0f, body);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 20; i++)
            frames.Run(0f, body);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
