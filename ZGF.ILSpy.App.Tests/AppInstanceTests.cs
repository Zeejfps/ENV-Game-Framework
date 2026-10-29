using ZGF.ILSpy.Core;
using static ZGF.ILSpy.App.Tests.TestPaths;

namespace ZGF.ILSpy.App.Tests;

public sealed class AppInstanceTests
{
    private const string GreetId = "M:ZGF.ILSpy.App.Tests.Fixtures.Greeter.Greet(System.Int32)";

    [Fact]
    public void The_first_run_with_some_settings_is_the_one_later_runs_hand_their_command_lines()
    {
        using var temp = new TempDir();
        var settings = temp.PathOf("settings.json");
        using var first = AppInstance.Claim(settings);
        first.Listen();
        using var later = AppInstance.Claim(settings);
        var woken = 0;
        first.WakeWith(() => Interlocked.Increment(ref woken));

        var handedOver = later.TryHandOver(CommandLine.Parse(["--navigateto=T:Some.Type", "relative.dll"]));

        Assert.True(first.IsFirst);
        Assert.False(later.IsFirst);
        Assert.True(handedOver);
        Assert.True(first.TryTakeHanded(out var handed));
        Assert.Equal("T:Some.Type", handed.NavigateTo);
        Assert.Equal([Path.GetFullPath("relative.dll")], handed.AssemblyPaths.Select(static p => p.Text));
        Assert.Equal(1, Volatile.Read(ref woken));
    }

    [Fact]
    public void Runs_with_other_settings_are_each_the_first()
    {
        using var temp = new TempDir();
        using var one = AppInstance.Claim(temp.PathOf("one.json"));
        using var other = AppInstance.Claim(temp.PathOf("other.json"));

        Assert.True(one.IsFirst);
        Assert.True(other.IsFirst);
    }

    [Fact]
    public void Once_the_first_run_ends_the_next_one_is_the_first()
    {
        using var temp = new TempDir();
        var settings = temp.PathOf("settings.json");
        var first = AppInstance.Claim(settings);
        first.Listen();
        first.Dispose();

        using var next = AppInstance.Claim(settings);

        Assert.True(next.IsFirst);
    }

    [Fact]
    public void A_command_line_handed_to_the_open_window_opens_its_files_shows_its_member_and_brings_the_window_to_the_front()
    {
        using var temp = new TempDir();
        var settings = temp.PathOf("settings.json");
        using var first = AppInstance.Claim(settings);
        first.Listen();
        var app = new AppDriver(instance: first);
        app.Run();
        using var later = AppInstance.Claim(settings);

        Assert.True(later.TryHandOver(CommandLine.Parse(["--navigateto=" + GreetId, AppStateDriver.FixturePath])));
        app.Run();
        var front = app.Frames.FrontRequested;
        app.RunUntil(() => app.AppState.Tabs.Active?.Node?.Kind == NodeKind.Method);
        app.Settle(1);

        Assert.True(front);
        Assert.Equal("Greet(int) : string", app.AppState.Tabs.Active!.Node!.Text);
        Assert.Equal([AppStateDriver.FixturePath], app.AppState.Assemblies.FilePaths.Select(static p => p.Text));
    }
}
