using System.IO.Compression;
using ZGF.ILSpy.Core;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.Navigation;

namespace ZGF.ILSpy.App.Tests;

public sealed class PackageBrowsingTests
{
    private const string PackageName = "Fixtures.1.0.0.nupkg";
    private const string LibFolder = "lib/net10.0";

    [Fact]
    public void An_opened_package_is_listed_in_the_tree_and_its_tab_lists_what_it_holds()
    {
        using var temp = new TempDir();
        var app = new AppDriver();

        OpenAssembly(app.AppState, WritePackage(temp));
        app.RunUntil(() => app.AppState.Tabs.Active?.Node?.Kind == NodeKind.Package);
        app.Settle(2);

        Assert.Equal(NodeKind.Package, Assert.Single(app.Ui.Tree.Roots).Kind);
        Assert.True(app.Frames.Drew(PackageName));
        Assert.Contains($"{LibFolder}/", app.AppState.Tabs.Active!.Document.GetText());
    }

    [Fact]
    public void Going_to_a_type_in_a_package_reveals_it_in_the_tree_under_the_package()
    {
        using var temp = new TempDir();
        var app = OpenedPackage(temp);
        var greeter = Greeter(app);

        Navigate(app.AppState, greeter.Reference!, inNewTab: false);
        app.Settle(2);

        Assert.Same(greeter, app.AppState.Tabs.Active!.Node);
        Assert.Same(greeter, app.Ui.Tree.View.Selected);
    }

    [Fact]
    public void Closing_from_a_node_inside_a_package_closes_the_package()
    {
        using var temp = new TempDir();
        var app = OpenedPackage(temp);

        CloseAssembly(app.AppState, Greeter(app).Assembly);
        app.Run(2);

        Assert.Empty(app.AppState.Assemblies.Listed);
        Assert.Empty(app.Ui.Tree.Roots);
    }

    [Fact]
    public void The_close_button_closes_a_selected_package_but_not_an_assembly_inside_it()
    {
        using var temp = new TempDir();
        var app = OpenedPackage(temp);
        var package = app.AppState.Assemblies.Listed[0];

        app.Ui.Tree.View.Select(Greeter(app).Assembly.Root);
        app.Run(2);
        app.Click(LucideIcons.Trash2);
        Assert.Single(app.AppState.Assemblies.Listed);

        app.Ui.Tree.View.Select(package.Root);
        app.Run(2);
        app.Click(LucideIcons.Trash2);

        Assert.Empty(app.AppState.Assemblies.Listed);
    }

    [Fact]
    public void An_assembly_in_a_package_is_named_in_results_with_the_folder_it_sits_in()
    {
        using var temp = new TempDir();
        var app = OpenedPackage(temp);
        var assembly = Greeter(app).Assembly;

        var label = NodeText.AssemblyLabel(assembly);

        Assert.Equal($"{assembly.NameAndVersion} · {LibFolder}", label);
    }

    [Fact]
    public void Exporting_offers_the_assemblies_of_an_open_package_with_the_picked_one_ticked()
    {
        using var temp = new TempDir();
        var app = OpenedPackage(temp);
        var assembly = Greeter(app).Assembly;

        app.OpenExportSetup(assembly);

        var choice = Assert.Single(app.Ui.ExportSetup.Choices);
        Assert.Same(assembly, choice.Assembly);
        Assert.True(choice.Included);
    }

    private static AppDriver OpenedPackage(TempDir temp)
    {
        var app = new AppDriver();
        OpenAssembly(app.AppState, WritePackage(temp));
        app.RunUntil(() => app.AppState.Tabs.Active?.Node?.Kind == NodeKind.Package);
        app.Settle(2);
        return app;
    }

    private static BrowseNode Greeter(AppDriver app)
    {
        var package = app.AppState.Assemblies.Listed[0];
        var fixtures = package.Root.Child(LibFolder).Children.Single(IsAssembly);
        return fixtures.Child(AppStateDriver.FixturesNamespace, "Greeter");
    }

    private static bool IsAssembly(BrowseNode node)
    {
        return node.Kind == NodeKind.Assembly;
    }

    /// <summary>A NuGet package of the fixture assembly, beside a text file.</summary>
    private static string WritePackage(TempDir temp)
    {
        var path = temp.Write(PackageName, []);
        using var archive = ZipFile.Open(path, ZipArchiveMode.Update);
        var name = Path.GetFileName(AppStateDriver.FixturePath);
        archive.CreateEntryFromFile(AppStateDriver.FixturePath, $"{LibFolder}/{name}");
        var readme = archive.CreateEntry("readme.txt");
        using var writer = new StreamWriter(readme.Open());
        writer.Write("Read me");
        return path;
    }
}
