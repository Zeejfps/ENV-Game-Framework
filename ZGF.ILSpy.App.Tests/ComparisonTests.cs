using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Comparison;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.Comparisons;
using static ZGF.ILSpy.Docking;
using static ZGF.ILSpy.Navigation;

namespace ZGF.ILSpy.App.Tests;

public sealed class ComparisonTests
{
    private const string LeftName = "ApiDiffSample (1.0.0.0)";
    private const string RightName = "ApiDiffSample (2.0.0.0)";
    private const string Compared = LeftName + "  →  " + RightName;

    private static readonly string LeftPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ApiDiff", "Older", "ApiDiffSample.dll");
    private static readonly string RightPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ApiDiff", "Newer", "ApiDiffSample.dll");

    /// <summary>A driver that opened both versions of the sample library, the left first.</summary>
    private static AppStateDriver OpenBoth()
    {
        var d = new AppStateDriver();
        OpenAssemblies(d.AppState, [LeftPath, RightPath]);
        d.RunUntil(() => d.AppState.Assemblies.Assemblies.Count == 2);
        d.Settle();
        return d;
    }

    private static LoadedAssembly Opened(AppStateDriver d, string path) => d.AppState.Assemblies.Assemblies.Single(a => a.FilePath == path);

    /// <summary>Compares <paramref name="assembly"/> with <paramref name="other"/> and waits for the comparison.</summary>
    private static AssemblyComparison Compare(AppStateDriver d, string assembly, string other)
    {
        CompareAssemblies(d.AppState, Opened(d, assembly), Opened(d, other));
        d.Settle();
        return d.AppState.Tabs.Active!.Comparison!;
    }

    private static ApiEntry Entry(ApiDiff diff, string namespaceName, string type)
    {
        var ns = diff.Namespaces.Single(n => n.Text == namespaceName);
        return ns.Children.Single(c => c.Text == type);
    }

    [Fact]
    public void Comparing_two_assemblies_shows_what_changed_in_a_tab_kept_open()
    {
        var d = OpenBoth();

        var comparison = Compare(d, LeftPath, RightPath);

        var tab = d.AppState.Tabs.Active!;
        Assert.Equal("ApiDiffSample API diff", tab.Title);
        Assert.NotSame(tab, d.AppState.Tabs.Preview);
        Assert.Equal(LeftName, comparison.Diff!.LeftName);
        Assert.Equal(RightName, comparison.Diff.RightName);
        Assert.Equal(4, comparison.Diff.Added);
        Assert.Empty(d.AppState.Errors.Entries);
    }

    [Fact]
    public void The_assemblies_keep_the_sides_they_were_picked_for_whatever_their_versions()
    {
        var d = OpenBoth();

        var comparison = Compare(d, RightPath, LeftPath);

        Assert.Equal(RightPath, comparison.LeftPath);
        Assert.Equal(RightName, comparison.Diff!.LeftName);
    }

    [Fact]
    public void Swapping_compares_the_other_way_round()
    {
        var d = OpenBoth();
        var comparison = Compare(d, LeftPath, RightPath);
        var added = comparison.Diff!.Added;
        var removed = comparison.Diff.Removed;

        SwapComparedAssemblies(d.AppState, comparison);
        d.Settle();

        Assert.Equal(RightName, comparison.Diff!.LeftName);
        Assert.Equal(removed, comparison.Diff.Added);
        Assert.Equal(added, comparison.Diff.Removed);
    }

    [Fact]
    public void Taking_in_all_that_is_declared_compares_again_with_what_is_not_public()
    {
        var d = OpenBoth();
        var comparison = Compare(d, LeftPath, RightPath);
        Assert.Equal(ApiChange.Removed, Entry(comparison.Diff!, "ApiDiffSample", "Helper").Change);

        ChangeComparedScope(d.AppState, comparison, ApiScope.AllDeclared);
        d.Settle();

        Assert.Equal(ApiScope.AllDeclared, comparison.Diff!.Scope);
        Assert.Equal(ApiChange.Changed, Entry(comparison.Diff, "ApiDiffSample", "Helper").Change);
    }

    [Fact]
    public void Going_to_an_entry_shows_its_code_in_another_tab_leaving_the_comparison_open()
    {
        var d = OpenBoth();
        d.TurnOffPreviewTabs();
        var comparison = Compare(d, LeftPath, RightPath);
        var comparisonTab = d.AppState.Tabs.Active!;
        var account = Entry(comparison.Diff!, "ApiDiffSample", "Account");

        Navigate(d.AppState, account.Right!, inNewTab: false);
        d.Settle();

        var shown = d.AppState.Tabs.Active!;
        Assert.NotSame(comparisonTab, shown);
        Assert.Equal("Account", shown.Node!.Text);
        Assert.Equal(RightPath, shown.Node.Assembly.FilePath);
        Assert.Same(comparison, comparisonTab.Comparison);
    }

    [Fact]
    public void Comparing_again_once_an_assembly_is_closed_says_it_is_no_longer_open()
    {
        var d = OpenBoth();
        var comparison = Compare(d, LeftPath, RightPath);

        CloseAssembly(d.AppState, Opened(d, RightPath));
        SwapComparedAssemblies(d.AppState, comparison);
        d.Settle();

        Assert.Null(comparison.Diff);
        Assert.Equal(new ComparisonFailure.AssemblyClosed("ApiDiffSample.dll"), comparison.Failure);
    }

    [Fact]
    public void Closing_the_tab_drops_the_comparison_in_flight()
    {
        var d = OpenBoth();
        CompareAssemblies(d.AppState, Opened(d, LeftPath), Opened(d, RightPath));
        var tab = d.AppState.Tabs.Active!;
        var comparison = tab.Comparison!;

        CloseTab(d.AppState, tab);
        d.Settle();

        Assert.True(comparison.IsClosed);
        Assert.Null(comparison.Diff);
        Assert.False(d.AppState.IsComparing);
    }

    [Fact]
    public void Compare_with_in_the_tree_menu_opens_a_dialog_on_the_assembly()
    {
        var app = OpenBothOnScreen();

        PickCompareWith(app);

        var picker = app.Ui.ComparisonPicker;
        Assert.True(picker.IsOpen);
        Assert.Equal(LeftPath, picker.Compared!.FilePath);
        Assert.True(app.Frames.Drew("Compare " + LeftName + " with"));
        Assert.True(app.Frames.Drew(RightName));
    }

    [Fact]
    public void Compare_in_the_dialog_compares_with_the_selected_assembly_in_a_new_tab()
    {
        var app = OpenBothOnScreen();
        PickCompareWith(app);

        app.ClickLast("Compare").Frame();
        app.RunUntil(() => app.AppState.Tabs.Active?.Comparison?.Diff != null);
        app.Settle(2);

        Assert.False(app.Ui.ComparisonPicker.IsOpen);
        Assert.True(app.Frames.Drew(Compared));
        Assert.True(app.Frames.Drew("4 added"));
        Assert.True(app.Frames.Drew("Account"));
        Assert.False(app.Frames.Drew("IStore"));
    }

    [Fact]
    public void The_swap_button_in_the_bar_over_the_document_swaps_the_sides()
    {
        var app = ComparedOnScreen();
        var comparison = app.AppState.Tabs.Active!.Comparison!;
        Assert.True(app.TextRect(app.Ui.Icons.ComparisonSwap).Y < app.TextRect(Compared).Y);

        app.Click(app.Ui.Icons.ComparisonSwap).Frame();
        app.RunUntil(() => comparison.Diff!.LeftName == RightName);
        app.Settle(2);

        Assert.Equal(LeftPath, comparison.RightPath);
        Assert.True(app.Frames.Drew(RightName + "  →  " + LeftName));
        Assert.True(app.Frames.Drew("4 removed"));
    }

    [Fact]
    public void The_tools_sit_in_the_bar_over_the_document_or_in_the_tab_while_that_bar_is_hidden()
    {
        var app = ComparedOnScreen();
        var bar = app.TextRect("Only differences");
        var summary = app.TextRect(Compared);
        Assert.True(bar.Y < summary.Y);

        app.Ui.Display = app.Ui.Display with { ShowOmnibar = false };
        app.Run(2);

        Assert.True(app.Frames.Drew("Only differences"));
    }

    /// <summary>An app that compared the two versions of the sample library, the older on the left.</summary>
    private static AppDriver ComparedOnScreen()
    {
        var app = OpenBothOnScreen();
        CompareAssemblies(app.AppState, Opened(app.Driver, LeftPath), Opened(app.Driver, RightPath));
        app.RunUntil(() => app.AppState.Tabs.Active?.Comparison?.Diff != null);
        app.Settle(2);
        return app;
    }

    private static AppDriver OpenBothOnScreen()
    {
        var app = new AppDriver(width: 1400f);
        OpenAssemblies(app.AppState, [LeftPath, RightPath]);
        app.RunUntil(() => app.AppState.Assemblies.Assemblies.Count == 2);
        app.Settle(2);
        return app;
    }

    /// <summary>Right-clicks the left assembly's row and picks Compare with… from its menu.</summary>
    private static void PickCompareWith(AppDriver app)
    {
        var row = app.TextRect("ApiDiffSample");
        app.Frames.Click(row.X + 2f, row.Y + row.Height / 2, PointerButton.Right).Frame();
        app.ClickMenuItem("Compare with…").Frame();
        app.Run(2);
    }

    [Fact]
    public void Showing_every_entry_draws_those_that_did_not_change_too()
    {
        var app = ComparedOnScreen();

        app.Click("Only differences").Frame();

        Assert.True(app.Frames.Drew("IStore"));
    }
}
