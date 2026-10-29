using ZGF.ILSpy.Core;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.AssemblyLists;
using static ZGF.ILSpy.Settings;

namespace ZGF.ILSpy.App.Tests;

public sealed class PickedPdbTests
{
    private static readonly string FixturePdb = Path.ChangeExtension(AppStateDriver.FixturePath, ".pdb");
    private static readonly string OtherBuildPdb = Path.ChangeExtension(typeof(AppState).Assembly.Location, ".pdb");

    [Fact]
    public void A_pdb_picked_from_the_tree_menu_is_read_after_the_reload()
    {
        using var temp = new TempDir();
        var copy = CopyFixtureWithoutPdb(temp);
        var app = new AppDriver(width: 1600f, settings: temp.Settings());
        OpenAssembly(app.AppState, copy);
        app.RunUntil(() => app.AppState.IsListed(copy));
        app.Settle(2);
        Assert.Null(OpenAssemblyOf(app.Driver).DebugSymbols);
        app.Frames.FilePicker.Answer = FixturePdb;

        app.PickFromTreeMenu(OpenAssemblyOf(app.Driver).Name, "Select PDB…");
        app.RunUntil(() => app.AppState.Assemblies.Pdbs.All.Count > 0);
        app.Settle(2);

        Assert.Equal(FixturePdb, OpenAssemblyOf(app.Driver).DebugSymbols!.SourceFileName);
        Assert.Empty(app.AppState.Errors.Entries);
    }

    [Fact]
    public void A_picked_pdb_is_kept_with_the_list_for_the_next_run()
    {
        using var temp = new TempDir();
        var copy = CopyFixtureWithoutPdb(temp);
        var d = OpenedCopy(temp, copy);

        Pick(d, FixturePdb);
        SaveSettings(d.AppState);
        var next = new AppStateDriver(temp.Settings());
        RestoreActiveAssemblyList(next.AppState);
        next.Settle();

        Assert.Equal(FixturePdb, Assert.Single(temp.Settings().AssemblyLists).Pdbs![copy]);
        Assert.Equal(FixturePdb, OpenAssemblyOf(next).DebugSymbols!.SourceFileName);
    }

    [Fact]
    public void A_pdb_of_another_build_is_reported_and_not_picked()
    {
        var d = AppStateDriver.Opened();

        UsePdb(d.AppState, OpenAssemblyOf(d), OtherBuildPdb);
        d.RunUntil(() => d.AppState.Errors.Entries.Count > 0);

        var error = Assert.Single(d.AppState.Errors.Entries);
        Assert.Empty(d.AppState.Assemblies.Pdbs.All);
        Assert.Contains("another build", error.Words().Details);
    }

    [Fact]
    public void Clearing_the_list_forgets_the_picked_pdbs()
    {
        using var temp = new TempDir();
        var copy = CopyFixtureWithoutPdb(temp);
        var d = OpenedCopy(temp, copy);
        Pick(d, FixturePdb);

        CloseAllAssemblies(d.AppState);
        RememberOpenAssemblies(d.AppState);
        SaveSettings(d.AppState);

        Assert.Empty(d.AppState.Assemblies.Pdbs.All);
        Assert.Null(Assert.Single(temp.Settings().AssemblyLists).Pdbs);
    }

    /// <summary>The fixture assembly copied where no PDB is beside it.</summary>
    private static string CopyFixtureWithoutPdb(TempDir temp)
    {
        return temp.Copy(AppStateDriver.FixturePath, Path.GetFileName(AppStateDriver.FixturePath));
    }

    private static AppStateDriver OpenedCopy(TempDir temp, string copy)
    {
        var d = new AppStateDriver(temp.Settings());
        OpenAssembly(d.AppState, copy);
        d.RunUntil(() => d.AppState.IsListed(copy));
        d.Settle();
        return d;
    }

    private static void Pick(AppStateDriver d, string pdb)
    {
        UsePdb(d.AppState, OpenAssemblyOf(d), pdb);
        d.RunUntil(() => d.AppState.Assemblies.Pdbs.All.Count > 0);
        d.Settle();
    }

    private static LoadedModule OpenAssemblyOf(AppStateDriver d) => Assert.Single(d.AppState.Assemblies.Assemblies);
}
