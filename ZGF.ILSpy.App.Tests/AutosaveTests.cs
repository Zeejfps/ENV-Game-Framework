using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy.App.Tests;

public sealed class AutosaveTests : IDisposable
{
    private readonly TempDir _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Preferences_changed_are_saved_with_the_next_frame()
    {
        var d = new WorkspaceDriver(_temp.Settings());
        d.Pump();

        d.Workspace.Theme = AppThemeId.Monokai;
        d.Workspace.SetLanguage(DecompilationLanguage.IL);
        d.Workspace.SetLanguageVersion(CSharpVersion.CSharp7_3);
        d.Workspace.ShowNonPublic = false;
        d.Workspace.Locale = Locale.De;
        d.Workspace.Search.Kinds = SearchKinds.Types;
        d.Pump();

        var saved = _temp.Settings();
        Assert.Equal(AppThemeId.Monokai, saved.Theme);
        Assert.Equal(DecompilationLanguage.IL, saved.Language);
        Assert.Equal(CSharpVersion.CSharp7_3, saved.Decompiler.LanguageVersion);
        Assert.False(saved.ShowNonPublic);
        Assert.Equal(Locale.De, saved.Locale);
        Assert.Equal(SearchKinds.Types, saved.SearchKinds);
    }

    [Fact]
    public void An_analysis_made_while_saved_ones_are_restoring_is_saved()
    {
        var first = WorkspaceDriver.Opened(_temp.Settings());
        first.Workspace.Analyze(first.Fixture("Greeter"));
        first.Pump();
        var next = new WorkspaceDriver(_temp.Settings());

        next.Workspace.Analyze(first.Fixture("Outline"));
        next.Settle();
        next.Pump();

        var saved = _temp.Settings().AssemblyLists.Single().Analyzed!;
        Assert.Equal(2, saved.Count);
    }

    [Fact]
    public void A_saved_analysis_that_is_not_found_again_leaves_the_settings_with_the_next_save()
    {
        var first = WorkspaceDriver.Opened(_temp.Settings());
        first.Workspace.Analyze(first.Fixture("Greeter"));
        first.Pump();
        var settings = _temp.Settings();
        var list = settings.AssemblyLists.Single();
        var anchor = list.Analyzed!.Single();
        settings.AssemblyLists[0] = list with { Analyzed = [anchor with { Token = anchor.Token + 0xFFFF }] };
        settings.Save();

        var next = new WorkspaceDriver(_temp.Settings());
        next.Settle();
        next.Pump();

        Assert.Empty(next.Workspace.Analyzer.Roots);
        Assert.Empty(_temp.Settings().AssemblyLists.Single().Analyzed ?? []);
    }

    /// <summary>Makes writing the settings fail on any platform: a folder stands where the save writes
    /// its temporary file. Returns the folder, to delete when writes should work again.</summary>
    private string BlockSettingsWrites()
    {
        var blocker = _temp.PathOf("settings.json.tmp");
        Directory.CreateDirectory(blocker);
        return blocker;
    }

    [Fact]
    public void A_failed_write_is_retried_after_a_wait_and_reported_once_until_one_succeeds()
    {
        var d = new WorkspaceDriver(_temp.Settings());
        d.Workspace.SaveSettings();
        var blocker = BlockSettingsWrites();

        d.Workspace.SetLanguage(DecompilationLanguage.IL);
        d.Pump();
        d.Now += SaveRetry.DelaySeconds + 0.5;
        d.Pump();
        Directory.Delete(blocker);
        d.Now += 1;
        d.Pump();
        var languageWhileWaiting = _temp.Settings().Language;
        d.Now += SaveRetry.DelaySeconds;
        d.Pump();

        Assert.Equal(DecompilationLanguage.CSharp, languageWhileWaiting);
        Assert.Equal(DecompilationLanguage.IL, _temp.Settings().Language);
        Assert.Equal("Cannot save the settings", Assert.Single(d.Workspace.Errors.Entries).Title);
    }

    [Fact]
    public void An_idle_app_wakes_to_retry_a_failed_write_once_the_wait_is_over()
    {
        var app = new AppDriver(settings: _temp.Settings());
        app.Run(2);
        var blocker = BlockSettingsWrites();

        app.Workspace.SetLanguage(DecompilationLanguage.IL);
        app.Run();
        var failedAt = app.Frames.RealTime;
        var wakeAt = app.Frames.WakeAt;
        app.Run();
        var languageWhileWaiting = _temp.Settings().Language;
        Directory.Delete(blocker);
        app.Frames.Wait(wakeAt - app.Frames.RealTime);
        app.Run();

        Assert.InRange(wakeAt - failedAt, 1.9, 2.1);
        Assert.Equal(DecompilationLanguage.CSharp, languageWhileWaiting);
        Assert.Equal(DecompilationLanguage.IL, _temp.Settings().Language);
        Assert.Single(app.Workspace.Errors.Entries);
    }

    [Fact]
    public void Saving_outright_as_shutting_down_does_writes_even_while_a_retry_waits()
    {
        var d = new WorkspaceDriver(_temp.Settings());
        var blocker = BlockSettingsWrites();
        d.Workspace.SetLanguage(DecompilationLanguage.IL);
        d.Pump();
        Directory.Delete(blocker);

        d.Workspace.SaveSettings();

        Assert.Equal(DecompilationLanguage.IL, _temp.Settings().Language);
    }
}
