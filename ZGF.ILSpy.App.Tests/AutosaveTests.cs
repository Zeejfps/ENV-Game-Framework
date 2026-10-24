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
}
