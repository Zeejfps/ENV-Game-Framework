using ZGF.ILSpy.Core;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.AssemblyLists;
using static ZGF.ILSpy.CommandLines;
using static ZGF.ILSpy.Settings;
using static ZGF.ILSpy.App.Tests.TestPaths;

namespace ZGF.ILSpy.App.Tests;

public sealed class CommandLineTests
{
    private const string Other = "Other";
    private static readonly string OtherAssembly = typeof(AppState).Assembly.Location;

    [Fact]
    public void Options_are_read_apart_from_the_files_to_open()
    {
        var commandLine = CommandLine.Parse(["--language=il", "--list= Work ", "--clear-list", "a.dll"]);

        Assert.Equal(DecompilationLanguage.IL, commandLine.Language);
        Assert.Equal("Work", commandLine.AssemblyList);
        Assert.True(commandLine.ClearList);
        Assert.Equal([Path.GetFullPath("a.dll")], commandLine.AssemblyPaths.Select(static p => p.Text));
    }

    [Fact]
    public void What_to_navigate_to_and_a_new_window_are_read_from_their_options()
    {
        var commandLine = CommandLine.Parse(["--navigateto= T:Some.Type ", "--newinstance"]);

        Assert.Equal("T:Some.Type", commandLine.NavigateTo);
        Assert.True(commandLine.NewInstance);
        Assert.Empty(commandLine.AssemblyPaths);
    }

    [Fact]
    public void A_command_line_is_handed_over_with_its_files_made_absolute_and_reads_back_the_same()
    {
        var commandLine = CommandLine.Parse(["--language=IL", "--list=Work", "--clear-list", "--navigateto=T:Some.Type", "--gl", "--newinstance", "a.dll"]);

        var handed = CommandLine.Parse(commandLine.ListHandedArguments().ToArray());

        Assert.Equal(DecompilationLanguage.IL, handed.Language);
        Assert.Equal("Work", handed.AssemblyList);
        Assert.True(handed.ClearList);
        Assert.Equal("T:Some.Type", handed.NavigateTo);
        Assert.False(handed.NewInstance);
        Assert.Equal([Path.GetFullPath("a.dll")], handed.AssemblyPaths.Select(static p => p.Text));
    }

    [Fact]
    public void A_command_line_naming_a_member_opens_its_files_and_shows_the_member()
    {
        var d = new AppStateDriver();

        OpenCommandLineFiles(d.AppState, CommandLine.Parse(["--navigateto=M:ZGF.ILSpy.App.Tests.Fixtures.Greeter.Greet(System.Int32)", AppStateDriver.FixturePath]));
        d.RunUntil(() => d.AppState.Tabs.Active?.Node != null);
        d.Settle();

        Assert.Equal("Greet(int) : string", d.AppState.Tabs.Active!.Node!.Text);
        Assert.Empty(d.AppState.Errors.Entries);
    }

    [Fact]
    public void A_command_line_naming_what_its_files_lack_reports_it()
    {
        var d = new AppStateDriver();

        OpenCommandLineFiles(d.AppState, CommandLine.Parse(["--navigateto=T:Nowhere.Missing", AppStateDriver.FixturePath]));
        d.RunUntil(() => d.AppState.Errors.Entries.Count > 0);

        Assert.Contains("T:Nowhere.Missing", Assert.Single(d.AppState.Errors.Entries).Words().Title);
    }

    [Fact]
    public void A_language_the_app_does_not_have_and_a_blank_list_name_are_left_out()
    {
        var commandLine = CommandLine.Parse(["--language=VB", "--list=  "]);

        Assert.Null(commandLine.Language);
        Assert.Null(commandLine.AssemblyList);
        Assert.False(commandLine.ClearList);
    }

    [Fact]
    public void The_language_named_is_the_one_decompiled_to()
    {
        var d = new AppStateDriver();

        Start(d, "--language=C#");
        var csharp = d.AppState.Language;
        Start(d, "--language=IL");

        Assert.Equal(DecompilationLanguage.CSharp, csharp);
        Assert.Equal(DecompilationLanguage.IL, d.AppState.Language);
    }

    [Fact]
    public void A_list_named_that_there_is_none_of_is_added_and_opened_empty()
    {
        using var temp = new TempDir();
        var d = new AppStateDriver(SettingsWith(temp, new AssemblyListRecord(SavedAssemblyLists.DefaultName, [PathOf(AppStateDriver.FixturePath)])));

        Start(d, $"--list={Other}");

        Assert.Equal(Other, d.AppState.Lists.Active);
        Assert.Empty(d.AppState.Assemblies.FilePaths);
        Assert.Equal([AppStateDriver.FixturePath], d.AppState.Lists.PathsOf(SavedAssemblyLists.DefaultName).Select(static p => p.Text));
    }

    [Fact]
    public void A_list_named_in_any_case_opens_with_its_assemblies_and_the_files_named()
    {
        using var temp = new TempDir();
        var settings = SettingsWith(temp,
            new AssemblyListRecord(SavedAssemblyLists.DefaultName, []),
            new AssemblyListRecord(Other, [PathOf(AppStateDriver.FixturePath)]));
        var d = new AppStateDriver(settings);

        Start(d, "--list=other", OtherAssembly);
        SaveSettings(d.AppState);

        AssemblyPath[] opened = [PathOf(AppStateDriver.FixturePath), PathOf(OtherAssembly)];
        Assert.Equal(Other, d.AppState.Lists.Active);
        Assert.Equal(opened.OrderBy(static p => p.Text), d.AppState.Assemblies.FilePaths.OrderBy(static p => p.Text));
        Assert.Equal(Other, temp.Settings().ActiveAssemblyList);
    }

    [Fact]
    public void Clearing_the_list_opens_only_the_files_named_and_saves_the_list_with_them()
    {
        using var temp = new TempDir();
        var d = new AppStateDriver(SettingsWith(temp, new AssemblyListRecord(SavedAssemblyLists.DefaultName, [PathOf(AppStateDriver.FixturePath)])));

        Start(d, "--clear-list", OtherAssembly);
        SaveSettings(d.AppState);

        Assert.Equal([OtherAssembly], d.AppState.Assemblies.FilePaths.Select(static p => p.Text));
        Assert.Equal([OtherAssembly], Assert.Single(temp.Settings().AssemblyLists).Assemblies.Select(static p => p.Text));
    }

    private static AppSettings SettingsWith(TempDir temp, params AssemblyListRecord[] lists)
    {
        var settings = temp.Settings();
        settings.AssemblyLists = lists.ToList();
        return settings;
    }

    /// <summary>Follows the command line of <paramref name="args"/>, then opens the list and the files
    /// it names, as the app does when it starts.</summary>
    private static void Start(AppStateDriver d, params string[] args)
    {
        var commandLine = CommandLine.Parse(args);
        FollowStartupCommandLine(d.AppState, commandLine);
        RestoreActiveAssemblyList(d.AppState);
        OpenAssemblies(d.AppState, commandLine.AssemblyPaths);
        d.RunUntil(() => AreListed(d.AppState, commandLine.AssemblyPaths));
        d.Settle();
    }

    private static bool AreListed(AppState appState, List<AssemblyPath> paths) => paths.All(appState.IsListed);
}
