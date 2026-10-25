using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using static ZGF.ILSpy.Assemblies;

namespace ZGF.ILSpy.App.Tests;

public sealed class AssemblyMenuTests
{
    private static readonly string CoreLibPath = typeof(object).Assembly.Location;

    [Fact]
    public void The_file_menu_sorts_the_list_by_name_and_closes_the_files_that_failed_to_load()
    {
        using var temp = new TempDir();
        var missing = temp.PathOf("Missing.dll");
        var app = AppDriver.Opened();
        OpenAssemblies(app.AppState, [missing, CoreLibPath]);
        app.RunUntil(() => app.AppState.Assemblies.Listed.Count == 3);
        app.Settle(2);

        app.Click("File").Frame(2);
        app.ClickMenuItem("Sort list by name").Frame(2);

        var sorted = app.AppState.Assemblies.FilePaths;
        Assert.Equal([missing, CoreLibPath, AppStateDriver.FixturePath], sorted);

        app.Click("File").Frame(2);
        app.ClickMenuItem("Close assemblies that failed to load").Frame(2);

        var roots = app.Ui.Tree.Roots;
        Assert.Equal([CoreLibPath, AppStateDriver.FixturePath], roots.Select(r => r.Assembly.FilePath));
        Assert.False(AnyFailedToLoad(app.AppState));
    }

    [Fact]
    public void The_tree_menu_loads_the_dependencies_of_an_assembly()
    {
        var app = AppDriver.Opened();
        app.Run(2);
        var name = app.AppState.Assemblies.Assemblies[0].Name;
        var row = app.TextRect(name);

        app.Frames.Click(row.X + 2f, row.Y + row.Height / 2, PointerButton.Right).Frame();
        app.ClickMenuItem("Load dependencies").Frame();
        app.RunUntil(() => app.AppState.Assemblies.Assemblies.Any(a => a.Name == "System.Runtime"));
        app.Settle(2);

        var names = app.Ui.Tree.Roots.Select(r => r.Assembly.Name).ToList();
        Assert.Equal(name, names[0]);
        Assert.Contains("System.Runtime", names);
        Assert.Contains("ZGF.ILSpy.Core", names);
    }
}
