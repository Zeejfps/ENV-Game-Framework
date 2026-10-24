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
}
