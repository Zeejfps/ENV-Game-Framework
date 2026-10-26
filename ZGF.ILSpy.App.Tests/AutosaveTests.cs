using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Preferences;
using static ZGF.ILSpy.Settings;

namespace ZGF.ILSpy.App.Tests;

public sealed class AutosaveTests : IDisposable
{
    private readonly TempDir _temp = new();

    public void Dispose() => _temp.Dispose();

    private static readonly WindowBounds MovedExportWindow = new(-900, 200, 700, 800, false);
    private static readonly WindowBounds MovedNuGetWindow = new(100, -300, 900, 600, false);

    [Theory]
    [InlineData(nameof(AppSettings.Theme))]
    [InlineData(nameof(AppSettings.Language))]
    [InlineData(nameof(AppSettings.Decompiler))]
    [InlineData(nameof(AppSettings.ApiVisibility))]
    [InlineData(nameof(AppSettings.Locale))]
    [InlineData(nameof(AppSettings.SearchKinds))]
    [InlineData(nameof(AppSettings.FoldedOptionGroups))]
    [InlineData(nameof(AppSettings.BookmarkColumns))]
    [InlineData(nameof(AppSettings.SearchColumns))]
    [InlineData(nameof(AppSettings.ExportColumns))]
    [InlineData(nameof(AppSettings.ExportWindow))]
    [InlineData(nameof(AppSettings.NuGetWindow))]
    public void A_saved_setting_changed_alone_is_saved_with_the_next_frame(string field)
    {
        var app = new AppDriver(settings: _temp.Settings());
        app.Run(2);

        Change(app, field);
        app.Run();

        Assert.True(IsSaved(_temp.Settings(), field), field);
    }

    private static void Change(AppDriver app, string field)
    {
        var appState = app.AppState;
        var screen = app.Ui;
        switch (field)
        {
            case nameof(AppSettings.Theme):
                appState.Theme = "monokai";
                break;
            case nameof(AppSettings.Language):
                SetLanguage(appState, DecompilationLanguage.IL);
                break;
            case nameof(AppSettings.Decompiler):
                SetLanguageVersion(appState, CSharpVersion.CSharp7_3);
                break;
            case nameof(AppSettings.ApiVisibility):
                SetApiVisibility(appState, ApiVisibility.All);
                break;
            case nameof(AppSettings.Locale):
                SetLocale(appState, Locale.De);
                break;
            case nameof(AppSettings.SearchKinds):
                screen.Search.Kinds = SearchKinds.Types;
                break;
            case nameof(AppSettings.FoldedOptionGroups):
                screen.FoldedOptionGroups.Fold(["Other"]);
                break;
            case nameof(AppSettings.BookmarkColumns):
                screen.BookmarkColumns.Restore([1f, 1f, 4f, 1f]);
                break;
            case nameof(AppSettings.SearchColumns):
                screen.SearchColumns.Restore([1f, 4f, 1f]);
                break;
            case nameof(AppSettings.ExportColumns):
                screen.ExportColumns.Restore([1f, 4f, 1f]);
                break;
            case nameof(AppSettings.ExportWindow):
                screen.ExportSetup.Window.Bounds = MovedExportWindow;
                break;
            case nameof(AppSettings.NuGetWindow):
                screen.NuGetPicker.Window.Bounds = MovedNuGetWindow;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field), field, null);
        }
    }

    private static bool IsSaved(AppSettings saved, string field) => field switch
    {
        nameof(AppSettings.Theme) => saved.Theme == "monokai",
        nameof(AppSettings.Language) => saved.Language == DecompilationLanguage.IL,
        nameof(AppSettings.Decompiler) => saved.Decompiler.LanguageVersion == CSharpVersion.CSharp7_3,
        nameof(AppSettings.ApiVisibility) => saved.ApiVisibility == ApiVisibility.All,
        nameof(AppSettings.Locale) => saved.Locale == Locale.De,
        nameof(AppSettings.SearchKinds) => saved.SearchKinds == SearchKinds.Types,
        nameof(AppSettings.FoldedOptionGroups) => saved.FoldedOptionGroups?.SequenceEqual(["Other"]) == true,
        nameof(AppSettings.BookmarkColumns) => saved.BookmarkColumns?.SequenceEqual([1f, 1f, 4f, 1f]) == true,
        nameof(AppSettings.SearchColumns) => saved.SearchColumns?.SequenceEqual([1f, 4f, 1f]) == true,
        nameof(AppSettings.ExportColumns) => saved.ExportColumns?.SequenceEqual([1f, 4f, 1f]) == true,
        nameof(AppSettings.ExportWindow) => saved.ExportWindow == MovedExportWindow,
        nameof(AppSettings.NuGetWindow) => saved.NuGetWindow == MovedNuGetWindow,
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, null),
    };

    [Fact]
    public void The_export_window_moved_is_saved_once_the_button_comes_up()
    {
        var app = new AppDriver(settings: _temp.Settings());
        app.Run(2);

        app.Frames.Press().Frame();
        app.Ui.ExportSetup.Window.Bounds = MovedExportWindow;
        app.Run(2);
        var savedWhileHeld = _temp.Settings().ExportWindow;
        app.Frames.Release().Frame();

        Assert.Null(savedWhileHeld);
        Assert.Equal(MovedExportWindow, _temp.Settings().ExportWindow);
    }

    [Fact]
    public void Closing_the_app_while_the_export_window_is_held_saves_where_it_is_now()
    {
        var app = new AppDriver(settings: _temp.Settings());
        app.Run(2);

        app.Frames.Press().Frame();
        app.Ui.ExportSetup.Window.Bounds = MovedExportWindow;
        app.Run(2);
        app.App.SaveOnExit(new WindowBounds(10, 20, 1000, 700, false));

        Assert.Equal(MovedExportWindow, _temp.Settings().ExportWindow);
    }

    [Fact]
    public void An_analysis_made_while_saved_ones_are_restoring_is_saved()
    {
        var first = AppStateDriver.Opened(_temp.Settings());
        first.Analyze(first.Fixture("Greeter"));
        first.Pump();
        var next = new AppStateDriver(_temp.Settings());

        next.Analyze(first.Fixture("Outline"));
        next.Settle();
        next.Pump();

        var saved = _temp.Settings().AssemblyLists.Single().Analyzed!;
        Assert.Equal(2, saved.Count);
    }

    [Fact]
    public void An_analysis_asked_for_by_reference_is_saved_once_it_is_found()
    {
        var d = AppStateDriver.Opened(_temp.Settings());
        d.Show(d.Fixture("Outline"));
        var document = d.AppState.Tabs.Active!.Document;
        var (line, column) = document.Find("return First();");
        var reference = document.SpanAt(line, column + "return ".Length).Reference!;

        d.Analyze(reference);
        SaveSettingsIfChanged(d.AppState, d.Now);
        var savedWhileFinding = _temp.Settings().AssemblyLists.Single().Analyzed?.Count ?? 0;
        d.Settle();
        d.Pump();

        Assert.Equal(0, savedWhileFinding);
        Assert.Single(_temp.Settings().AssemblyLists.Single().Analyzed!);
    }

    [Fact]
    public void A_saved_analysis_that_is_not_found_again_leaves_the_settings_with_the_next_save()
    {
        var first = AppStateDriver.Opened(_temp.Settings());
        first.Analyze(first.Fixture("Greeter"));
        first.Pump();
        var settings = _temp.Settings();
        var list = settings.AssemblyLists.Single();
        var anchor = list.Analyzed!.Single();
        settings.AssemblyLists[0] = list with { Analyzed = [anchor with { Token = anchor.Token + 0xFFFF }] };
        settings.Save();

        var next = new AppStateDriver(_temp.Settings());
        next.Settle();
        next.Pump();

        Assert.Empty(next.AppState.Analyzer.Roots);
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
        var d = new AppStateDriver(_temp.Settings());
        SaveSettings(d.AppState);
        var blocker = BlockSettingsWrites();

        SetLanguage(d.AppState, DecompilationLanguage.IL);
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
        Assert.Equal("Cannot save the settings", Assert.Single(d.AppState.Errors.Entries).Title);
    }

    [Fact]
    public void An_idle_app_wakes_to_retry_a_failed_write_once_the_wait_is_over()
    {
        var app = new AppDriver(settings: _temp.Settings());
        app.Run(2);
        var blocker = BlockSettingsWrites();

        SetLanguage(app.AppState, DecompilationLanguage.IL);
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
        Assert.Single(app.AppState.Errors.Entries);
    }

    [Fact]
    public void Saving_outright_as_shutting_down_does_writes_even_while_a_retry_waits()
    {
        var d = new AppStateDriver(_temp.Settings());
        var blocker = BlockSettingsWrites();
        SetLanguage(d.AppState, DecompilationLanguage.IL);
        d.Pump();
        Directory.Delete(blocker);

        SaveSettings(d.AppState);

        Assert.Equal(DecompilationLanguage.IL, _temp.Settings().Language);
    }

    [Fact]
    public void A_splitter_drag_that_ends_is_saved_with_the_next_frame()
    {
        var app = new AppDriver(settings: _temp.Settings());
        app.Run(2);
        var root = (DockSplit)app.Ui.Layout.Root;
        var before = root.Ratio;
        var divider = FindDivider(app, root);

        app.Frames.Drag(new PointF(divider, 350f), new PointF(divider + 20f, 350f), new PointF(divider + 120f, 350f));
        app.Frames.Release().Frame(2);

        var layout = _temp.Settings().Layout;
        Assert.NotNull(layout);
        var saved = Assert.IsType<DockSplit>(layout.Root);
        Assert.NotEqual(before, root.Ratio);
        Assert.Equal(root.Ratio, saved.Ratio, 3);
    }

    /// <summary>Where the pointer turns into the splitter's resize cursor, near where the root split's ratio puts it.</summary>
    private static float FindDivider(AppDriver app, DockSplit root)
    {
        var guess = root.Ratio * app.Frames.Width;
        for (var x = guess - 40f; x < guess + 40f; x++)
        {
            app.Frames.MovePointer(x, 350f).Frame();
            if (app.Frames.Cursor == MouseCursor.ResizeHorizontal)
                return x;
        }
        throw new InvalidOperationException("No splitter near the root split's ratio.");
    }
}
