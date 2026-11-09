using System.Diagnostics;
using Xunit.Abstractions;
using ZGF.Gui.Immediate;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Automation.Tests;

/// <summary>The acceptance flows of an app driven from another process: open a file through a queued picker answer,
/// drag a splitter, and open a popup and a dialog without a native window being shown or activated.</summary>
public sealed class RemoteAppTests(ITestOutputHelper output)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

    [SampleAppFact]
    public void Opening_a_file_answers_the_queued_picker_and_shows_the_file()
    {
        var file = Path.Combine(Path.GetTempPath(), $"zgf-remote-{Guid.NewGuid():N}", "groceries.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllLines(file, ["Buy milk", "Call Bob"]);
        using var app = RemoteApp.Launch(SampleApp.Launch, output.WriteLine);

        app.QueueFilePickerAnswer(file);
        app.ClickOn(Selector.AtPath("file"));
        app.ClickOn(Selector.AtPath("file/menu-open"));

        app.WaitFor(Selector.WithText("groceries.txt"), Timeout);
        app.WaitFor(Selector.WithText("Buy milk, Call Bob"), Timeout);
        Assert.Contains("5 tasks remaining", app.Snapshot());
        var request = Assert.Single(app.FilePickerRequests());
        Assert.Equal(FilePickerKind.File, request.Kind);
        Assert.Equal(file, request.Answer);
        var look = app.Look("remote-open-file");
        Assert.True(new FileInfo(look.Png).Length > 0);
        Assert.Contains("groceries.txt", File.ReadAllText(look.Text));
    }

    [SampleAppFact]
    public void Dragging_the_splitter_resizes_the_panes()
    {
        using var app = RemoteApp.Launch(SampleApp.Launch, output.WriteLine);
        var before = app.WaitFor(Selector.AtPath("items"), Timeout).Bounds;
        var dividerY = before.Y + before.Height + 3f;

        app.DragAndRelease(new WindowPoint(200f, dividerY), new WindowPoint(200f, dividerY + 20f));

        var after = app.WaitFor(Selector.AtPath("items"), Timeout).Bounds;
        Assert.Equal(before.Height + 20f, after.Height, 1f);
        app.Look("remote-split");
        var crop = app.Look("remote-split-list", Selector.AtPath("items"), margin: 4f, magnify: 2);
        Assert.StartsWith("# path=\"items\"", File.ReadAllText(crop.Text));
    }

    [SampleAppFact]
    public void A_popup_and_a_dialog_open_and_close_with_no_platform_effect_recorded()
    {
        using var app = RemoteApp.Launch(SampleApp.Launch, output.WriteLine);

        OpenPopupThenDialog(app);

        Assert.Empty(app.Effects());
    }

    [SampleAppWindowsFact]
    public void A_popup_and_a_dialog_open_without_a_native_window_shown_or_activated_from_startup_on()
    {
        using var watch = ProcessWindowWatch.Start();
        using var app = RemoteApp.Launch(SampleApp.Launch, output.WriteLine);
        watch.Follow(app.ProcessId);

        OpenPopupThenDialog(app);

        Assert.True(watch.WindowCount > 0, "The watch found none of the app's windows, so it proves nothing.");
        Assert.Empty(watch.Violations(app.ProcessId));
    }

    private static void OpenPopupThenDialog(RemoteApp app)
    {
        app.ClickOn(Selector.AtPath("file"));
        app.WaitFor(Selector.AtPath("file/menu-export"), Timeout);
        Assert.Contains("# surface popup", app.Snapshot());
        app.Look("remote-popup");

        app.ClickOn(Selector.AtPath("file/menu-export"));
        app.WaitFor(Selector.WithText("Tasks are exported as plain text, one per line."), Timeout);
        app.Settle();
        Assert.Contains("# surface window", app.Snapshot());
        app.Look("remote-dialog");

        app.ClickOn(Selector.AtPath("export-done"));
        app.Settle();
        Assert.DoesNotContain("# surface window", app.Snapshot());
    }

    [SampleAppFact]
    public void A_look_is_named_by_a_plain_file_name()
    {
        using var app = RemoteApp.Launch(SampleApp.Launch, output.WriteLine);

        foreach (var name in new[] { "../escape", "..", ".", "a/b", "a\\b", "", Path.Combine(Path.GetTempPath(), "rooted") })
            Assert.Throws<ArgumentException>(() => app.Look(name));
        Assert.True(File.Exists(app.Look("plain-name").Png));
    }

    [SampleAppFact]
    public void A_command_the_app_cannot_use_is_rejected_and_a_missing_node_fails_with_a_look()
    {
        using var app = RemoteApp.Launch(SampleApp.Launch, output.WriteLine);

        var rejected = app.Send(new AutomationCommand.Capture(new CaptureTarget.Surface(7)));
        Assert.IsType<AutomationResult.Rejected>(rejected);

        var failure = Assert.Throws<RemoteAppException>(() => app.ClickOn(Selector.WithText("No such button")));
        Assert.IsType<AutomationResult.NotFound>(failure.Result);
        Assert.Contains("Look at the moment it failed", failure.Message);
        Assert.Contains("textfield path=\"draft\"", failure.Message);

        var timedOut = Assert.Throws<TimeoutException>(() => app.WaitFor(Selector.WithText("Never shown"), TimeSpan.FromMilliseconds(300)));
        Assert.Contains("Nothing matches", timedOut.Message);
    }

    [SampleAppFact]
    public void Typing_into_a_field_adds_a_task()
    {
        using var app = RemoteApp.Launch(SampleApp.Launch, output.WriteLine);

        app.ClickOn(Selector.AtPath("draft"));
        app.Type("Write the remote test");
        app.Key(KeyboardKey.Enter);

        app.WaitFor(Selector.WithText("4 tasks remaining"), Timeout);
    }

    [SampleAppFact]
    public void Timings()
    {
        var launch = Stopwatch.StartNew();
        using var app = RemoteApp.Launch(SampleApp.Launch, output.WriteLine);
        var ready = launch.Elapsed;

        const int Rounds = 50;
        var step = Stopwatch.StartNew();
        for (var i = 0; i < Rounds; i++)
            app.Step();
        var perStep = step.Elapsed / Rounds;

        var snapshot = Stopwatch.StartNew();
        for (var i = 0; i < Rounds; i++)
            app.Snapshot();
        var perSnapshot = snapshot.Elapsed / Rounds;

        var click = Stopwatch.StartNew();
        for (var i = 0; i < 10; i++)
            app.ClickOn(Selector.AtPath("hide-completed"));
        var perClick = click.Elapsed / 10;

        app.Look("remote-timing-warmup");
        var look = Stopwatch.StartNew();
        for (var i = 0; i < 5; i++)
            app.Look("remote-timing");
        var perLook = look.Elapsed / 5;

        output.WriteLine($"launch to ready {ready.TotalMilliseconds:0} ms; step {perStep.TotalMilliseconds:0.00} ms; "
                         + $"snapshot {perSnapshot.TotalMilliseconds:0.00} ms; click on {perClick.TotalMilliseconds:0.00} ms; look {perLook.TotalMilliseconds:0.0} ms");
    }
}
