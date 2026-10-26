using ZGF.Gui;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using static ZGF.ILSpy.AssemblyLists;
using static ZGF.ILSpy.Settings;

namespace ZGF.ILSpy.App.Tests;

public sealed class TargetFrameworkTests
{
    private const string Net48 = ".NETFramework,Version=v4.8";

    private static LoadedAssembly FixturesOf(AppState appState) =>
        appState.Assemblies.Assemblies.Single(a => a.FilePath == AppStateDriver.FixturePath);

    /// <summary>Right-clicks the fixture assembly's row and picks <paramref name="item"/> from its
    /// Target framework submenu, then waits for the reload.</summary>
    private static void PickFramework(AppDriver app, string item)
    {
        var row = app.TextRect(FixturesOf(app.AppState).Name);
        app.Frames.Click(row.X + 2f, row.Y + row.Height / 2, PointerButton.Right).Frame();
        app.ClickMenuItem("Target framework").Frame();
        app.ClickMenuItem(item).Frame();
        app.Settle(2);
    }

    [Fact]
    public void A_framework_picked_from_the_tree_menu_resolves_the_assembly_after_the_reload_and_shows_in_its_row()
    {
        using var temp = new TempDir();
        var app = new AppDriver(width: 1600f, settings: temp.Settings());
        app.Driver.OpenFixtures();
        app.Settle(2);

        PickFramework(app, "net48");
        SaveSettings(app.AppState);

        var assembly = FixturesOf(app.AppState);
        Assert.True(assembly.IsTargetFrameworkPicked);
        Assert.Equal(Net48, assembly.TargetFramework);
        Assert.Equal("net48", NodeText.PickedFramework(assembly.Root));
        Assert.DoesNotContain("net48", NodeText.Secondary(assembly.Root));
        Assert.Equal(FontWeight.Bold, app.Frames.Canvas.Texts.Single(t => t.Inputs.Text == "net48").Inputs.Style.FontWeight.Value);
        Assert.Equal(Net48, Assert.Single(temp.Settings().AssemblyLists).Frameworks![AppStateDriver.FixturePath]);
    }

    [Fact]
    public void Picking_detected_goes_back_to_the_framework_the_assembly_was_built_for()
    {
        var app = AppDriver.Opened(width: 1600f);
        app.Settle(2);
        var detected = FrameworkIds.MonikerOf(FixturesOf(app.AppState).DetectedTargetFramework!);
        PickFramework(app, "net48");

        PickFramework(app, $"Detected ({detected})");

        var assembly = FixturesOf(app.AppState);
        Assert.False(assembly.IsTargetFrameworkPicked);
        Assert.Equal(assembly.DetectedTargetFramework, assembly.TargetFramework);
        Assert.Null(NodeText.PickedFramework(assembly.Root));
        Assert.Contains(detected, NodeText.Secondary(assembly.Root));
        Assert.Empty(app.AppState.Assemblies.TargetFrameworks.All);
    }

    [Fact]
    public void A_picked_framework_is_kept_with_the_list_for_the_next_run()
    {
        using var temp = new TempDir();
        var settings = temp.Settings();
        settings.AssemblyLists =
        [
            new AssemblyListRecord(SavedAssemblyLists.DefaultName, [AppStateDriver.FixturePath],
                Frameworks: new Dictionary<string, string> { [AppStateDriver.FixturePath] = Net48 }),
        ];
        var d = new AppStateDriver(settings);

        RestoreActiveAssemblyList(d.AppState);
        d.Settle();

        Assert.Equal(Net48, FixturesOf(d.AppState).TargetFramework);
    }
}
