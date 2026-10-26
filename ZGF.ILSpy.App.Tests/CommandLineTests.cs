using ZGF.ILSpy.Core;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.AssemblyLists;
using static ZGF.ILSpy.Settings;

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
        Assert.Equal(["a.dll"], commandLine.AssemblyPaths);
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
        var d = new AppStateDriver(SettingsWith(temp, new AssemblyListRecord(SavedAssemblyLists.DefaultName, [AppStateDriver.FixturePath])));

        Start(d, $"--list={Other}");

        Assert.Equal(Other, d.AppState.Lists.Active);
        Assert.Empty(d.AppState.Assemblies.FilePaths);
        Assert.Equal([AppStateDriver.FixturePath], d.AppState.Lists.PathsOf(SavedAssemblyLists.DefaultName));
    }

    [Fact]
    public void A_list_named_in_any_case_opens_with_its_assemblies_and_the_files_named()
    {
        using var temp = new TempDir();
        var settings = SettingsWith(temp,
            new AssemblyListRecord(SavedAssemblyLists.DefaultName, []),
            new AssemblyListRecord(Other, [AppStateDriver.FixturePath]));
        var d = new AppStateDriver(settings);

        Start(d, "--list=other", OtherAssembly);
        SaveSettings(d.AppState);

        string[] opened = [AppStateDriver.FixturePath, OtherAssembly];
        Assert.Equal(Other, d.AppState.Lists.Active);
        Assert.Equal(opened.Order(), d.AppState.Assemblies.FilePaths.Order());
        Assert.Equal(Other, temp.Settings().ActiveAssemblyList);
    }

    [Fact]
    public void Clearing_the_list_opens_only_the_files_named_and_saves_the_list_with_them()
    {
        using var temp = new TempDir();
        var d = new AppStateDriver(SettingsWith(temp, new AssemblyListRecord(SavedAssemblyLists.DefaultName, [AppStateDriver.FixturePath])));

        Start(d, "--clear-list", OtherAssembly);
        SaveSettings(d.AppState);

        Assert.Equal([OtherAssembly], d.AppState.Assemblies.FilePaths);
        Assert.Equal([OtherAssembly], Assert.Single(temp.Settings().AssemblyLists).Assemblies);
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
        ILSpyAppLauncher.FollowCommandLine(d.AppState, commandLine);
        RestoreActiveAssemblyList(d.AppState);
        OpenAssemblies(d.AppState, commandLine.AssemblyPaths);
        d.RunUntil(() => AreListed(d.AppState, commandLine.AssemblyPaths));
        d.Settle();
    }

    private static bool AreListed(AppState appState, List<string> paths) => paths.All(appState.IsListed);
}
