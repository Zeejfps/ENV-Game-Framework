using ZGF.Desktop;
using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy.App.Tests;

public sealed class ExportTests : IDisposable
{
    private readonly TempDir _temp = new();

    public ExportTests()
    {
        Directory.CreateDirectory(_temp.Path);
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Saving_code_writes_the_node_in_the_current_language()
    {
        var d = WorkspaceDriver.Opened();
        d.Workspace.SetLanguage(DecompilationLanguage.IL);
        var path = _temp.PathOf("Greeter.il");

        d.Workspace.SaveCode(d.Fixture("Greeter"), path);
        d.Settle();

        Assert.Contains(".class public auto ansi beforefieldinit ZGF.ILSpy.App.Tests.Fixtures.Greeter", File.ReadAllText(path));
        Assert.Empty(d.Workspace.Errors.Entries);
    }

    [Fact]
    public void Saving_an_assembly_as_a_project_file_exports_the_project()
    {
        var d = WorkspaceDriver.Opened();
        var path = _temp.PathOf(Path.Combine("Project", "Fixtures.csproj"));

        d.Workspace.SaveCode(d.Node(), path);
        d.Settle();

        Assert.True(File.Exists(path));
        Assert.Contains(ProjectFiles(), f => Path.GetFileName(f) == "Greeter.cs");
    }

    [Fact]
    public void An_export_into_a_folder_with_other_files_waits_until_the_user_agrees()
    {
        var d = WorkspaceDriver.Opened();
        _temp.Write("notes.txt", "keep");
        var path = _temp.PathOf("Fixtures.csproj");

        d.Workspace.ExportProject(d.Node().Assembly, path);
        d.Settle();
        Assert.False(File.Exists(path));
        Assert.Equal(_temp.Path, d.Workspace.ConfirmingExport!.Folder);

        d.Workspace.FinishExportConfirmation(true);
        d.Settle();

        Assert.Null(d.Workspace.ConfirmingExport);
        Assert.True(File.Exists(path));
        Assert.Equal("keep", File.ReadAllText(_temp.PathOf("notes.txt")));
    }

    [Fact]
    public void A_declined_export_writes_nothing()
    {
        var d = WorkspaceDriver.Opened();
        _temp.Write("notes.txt", "keep");
        var path = _temp.PathOf("Fixtures.csproj");

        d.Workspace.ExportProject(d.Node().Assembly, path);
        d.Workspace.FinishExportConfirmation(false);
        d.Settle();

        Assert.Null(d.Workspace.ConfirmingExport);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Pdbs_are_generated_for_every_open_assembly_into_the_folder()
    {
        var d = WorkspaceDriver.Opened();
        var assembly = d.Node().Assembly;

        d.Workspace.GeneratePdbs(_temp.Path);
        d.Settle();

        Assert.True(File.Exists(_temp.PathOf(assembly.PdbFileName)));
        Assert.Empty(d.Workspace.Errors.Entries);
    }

    [Fact]
    public void Ctrl_S_saves_the_active_tab_where_the_user_picks()
    {
        var app = AppDriver.Opened();
        app.Show(app.Fixture("Greeter"), frames: 2);
        var path = _temp.PathOf("Greeter.cs");
        app.Frames.FilePicker.Answer = path;

        app.Frames.Key(KeyboardKey.S, KeyModifiers.Control).Frame(2);
        app.Settle();

        Assert.Equal(["Save code"], app.Frames.FilePicker.Titles);
        Assert.Contains("public class Greeter", File.ReadAllText(path));
    }

    [Fact]
    public void The_dialog_asks_before_exporting_into_a_folder_with_other_files()
    {
        var app = AppDriver.Opened();
        _temp.Write("notes.txt", "keep");
        var path = _temp.PathOf("Fixtures.csproj");
        app.Workspace.ExportProject(app.Node().Assembly, path);
        app.Run(2);

        Assert.True(app.Frames.Drew("Folder is not empty"));
        app.Click("Export").Frame(2);
        app.Settle(1);

        Assert.True(File.Exists(path));
        Assert.False(app.Frames.Drew("Folder is not empty"));
    }

    private IEnumerable<string> ProjectFiles() => Directory.EnumerateFiles(_temp.Path, "*.cs", SearchOption.AllDirectories);
}
