using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Comparison;

namespace ZGF.ILSpy.Tests;

public sealed class ApiDiffTests
{
    private const string SampleNamespace = "ApiDiffSample";

    private static readonly Lazy<(AssemblyList List, LoadedAssembly Left, LoadedAssembly Right)> s_opened = new(OpenBoth);
    private static readonly Lazy<ApiDiff> s_public = new(() => Compare(ApiScope.PublicApi));
    private static readonly Lazy<ApiDiff> s_all = new(() => Compare(ApiScope.AllDeclared));

    private static (AssemblyList List, LoadedAssembly Left, LoadedAssembly Right) OpenBoth()
    {
        var list = new AssemblyList();
        var left = list.Open(Trees.ApiDiffOlderPath);
        var right = list.Open(Trees.ApiDiffNewerPath);
        return (list, left, right);
    }

    private static ApiDiff Compare(ApiScope scope)
    {
        var opened = s_opened.Value;
        return ApiDiff.Compare(opened.Left, opened.Right, scope, CancellationToken.None);
    }

    private static ApiEntry Entry(ApiDiff diff, params ReadOnlySpan<string> path)
    {
        var entries = diff.Namespaces;
        ApiEntry? entry = null;
        foreach (var text in path)
        {
            entry = Named(entries, text);
            entries = entry.Children;
        }
        return entry!;
    }

    private static ApiEntry Named(IReadOnlyList<ApiEntry> entries, string text)
    {
        var found = entries.FirstOrDefault(e => e.Text == text);
        if (found == null)
            throw new InvalidOperationException($"No entry '{text}' among: {string.Join(", ", entries.Select(e => e.Text))}");
        return found;
    }

    private static ApiEntry Public(params ReadOnlySpan<string> path) => Entry(s_public.Value, path);

    private static ApiEntry All(params ReadOnlySpan<string> path) => Entry(s_all.Value, path);

    [Fact]
    public void The_comparison_names_both_assemblies_with_their_versions()
    {
        var diff = s_public.Value;

        Assert.Equal("ApiDiffSample (1.0.0.0)", diff.LeftName);
        Assert.Equal("ApiDiffSample (2.0.0.0)", diff.RightName);
    }

    [Fact]
    public void What_is_declared_alike_on_both_sides_is_unchanged_and_points_at_both()
    {
        var deposit = Public(SampleNamespace, "Account", "Deposit(decimal) : void");
        var statement = Public(SampleNamespace, "Account", "Statement");

        Assert.Equal(ApiChange.Unchanged, deposit.Change);
        Assert.False(deposit.HasChanges);
        Assert.False(statement.HasChanges);
        Assert.NotNull(deposit.Left);
        Assert.NotNull(deposit.Right);
        Assert.Equal(deposit.LeftDeclaration, deposit.RightDeclaration);
    }

    [Fact]
    public void A_member_on_one_side_only_is_added_or_removed_and_points_at_that_side()
    {
        var transfer = Public(SampleNamespace, "Account", "Transfer(Account, decimal) : void");
        var withdraw = Public(SampleNamespace, "Account", "Withdraw(decimal) : void");

        Assert.Equal(ApiChange.Added, transfer.Change);
        Assert.Null(transfer.Left);
        Assert.NotNull(transfer.Right);
        Assert.Equal(ApiChange.Removed, withdraw.Change);
        Assert.NotNull(withdraw.Left);
        Assert.Null(withdraw.Right);
        Assert.Null(withdraw.RightDeclaration);
    }

    [Fact]
    public void Each_overload_is_matched_alone()
    {
        var ledger = Public(SampleNamespace, "Ledger");

        Assert.Equal(ApiChange.Unchanged, Named(ledger.Children, "Add(int) : void").Change);
        Assert.Equal(ApiChange.Removed, Named(ledger.Children, "Add(string) : void").Change);
        Assert.Equal(ApiChange.Added, Named(ledger.Children, "Add(long) : void").Change);
    }

    [Fact]
    public void A_member_declared_differently_is_changed_with_both_declarations()
    {
        var describe = Public(SampleNamespace, "Account", "Describe() : string");
        var balance = Public(SampleNamespace, "Account", "Balance : decimal");
        var maxOwners = Public(SampleNamespace, "Account", "MaxOwners : int");

        Assert.Equal(ApiChange.Changed, describe.Change);
        Assert.Equal("public virtual string Describe()", describe.LeftDeclaration);
        Assert.Equal("public string Describe()", describe.RightDeclaration);
        Assert.Equal(ApiChange.Changed, balance.Change);
        Assert.Equal("public decimal Balance { get; private set; }", balance.LeftDeclaration);
        Assert.Equal("public decimal Balance { get; set; }", balance.RightDeclaration);
        Assert.Equal(ApiChange.Changed, maxOwners.Change);
        Assert.Equal("public const int MaxOwners = 2", maxOwners.LeftDeclaration);
        Assert.Equal("public const int MaxOwners = 3", maxOwners.RightDeclaration);
    }

    [Fact]
    public void A_type_made_obsolete_is_changed()
    {
        var legacy = Public(SampleNamespace, "Legacy");

        Assert.Equal(ApiChange.Changed, legacy.Change);
        Assert.Equal("[Obsolete(\"Use Account.Describe\")] public static class Legacy", legacy.RightDeclaration);
        Assert.Equal(ApiChange.Unchanged, Named(legacy.Children, "Run() : void").Change);
    }

    [Fact]
    public void A_parameter_that_turns_nullable_changes_its_member_rather_than_replacing_it()
    {
        var note = Public(SampleNamespace, "Notes", "Note(string?) : void");

        Assert.Equal(ApiChange.Changed, note.Change);
        Assert.Equal("public void Note(string)", note.LeftDeclaration);
        Assert.Equal("public void Note(string?)", note.RightDeclaration);
    }

    [Fact]
    public void A_type_that_only_gained_a_member_is_unchanged_itself_but_has_changes()
    {
        var currency = Public(SampleNamespace, "Currency");

        Assert.Equal(ApiChange.Unchanged, currency.Change);
        Assert.True(currency.HasChanges);
        Assert.Equal(ApiChange.Added, Named(currency.Children, "Pound : Currency").Change);
        Assert.DoesNotContain(currency.Children, static c => c.Text.StartsWith("value__"));
    }

    [Fact]
    public void A_namespace_on_one_side_only_is_added_or_removed_with_its_types()
    {
        var reports = Public("ApiDiffSample.Reports");
        var old = Public("ApiDiffSample.Old");

        Assert.Equal(ApiChange.Added, reports.Change);
        Assert.Equal(ApiChange.Added, Named(reports.Children, "Summary").Change);
        Assert.Equal(ApiChange.Removed, old.Change);
        Assert.Equal(ApiChange.Removed, Named(old.Children, "Retired").Change);
        Assert.Equal(ApiChange.Removed, Entry(s_public.Value, "ApiDiffSample.Old", "Retired", "Close() : void").Change);
    }

    [Fact]
    public void The_public_api_leaves_out_what_other_assemblies_cannot_use()
    {
        var account = Public(SampleNamespace, "Account");

        Assert.Equal(ApiChange.Removed, Public(SampleNamespace, "Helper").Change);
        Assert.DoesNotContain(account.Children, static c => c.Text.StartsWith("Audit"));
    }

    [Fact]
    public void All_that_is_declared_takes_in_what_is_not_public_and_never_what_the_compiler_made_up()
    {
        var account = All(SampleNamespace, "Account");

        Assert.Equal(ApiChange.Changed, All(SampleNamespace, "Helper").Change);
        Assert.Equal(ApiChange.Added, Named(account.Children, "AuditCount : int").Change);
        Assert.Equal(ApiChange.Unchanged, Named(account.Children, "Audit() : void").Change);
        Assert.DoesNotContain(account.Children, static c => c.Text.Contains("k__BackingField"));
        Assert.DoesNotContain(s_all.Value.Namespaces.SelectMany(static n => n.Children), static c => c.Text == "<Module>");
    }

    [Fact]
    public void Entries_sort_as_the_tree_sorts_them_types_then_fields_properties_and_methods_by_name()
    {
        var account = Public(SampleNamespace, "Account");

        var texts = account.Children
            .Select(static c => c.Text)
            .ToList();

        Assert.Equal("Statement", texts[0]);
        Assert.Equal("MaxOwners : int", texts[1]);
        Assert.True(texts.IndexOf("Owner : string") < texts.IndexOf("Deposit(decimal) : void"));
        Assert.True(texts.IndexOf("Deposit(decimal) : void") < texts.IndexOf("Withdraw(decimal) : void"));
    }

    [Fact]
    public void An_added_or_removed_type_counts_once_without_its_members()
    {
        var diff = s_public.Value;

        Assert.Equal(4, diff.Added);
        Assert.Equal(4, diff.Removed);
        Assert.Equal(5, diff.Changed);
    }

    [Fact]
    public void An_entry_points_at_its_definition_in_each_assembly()
    {
        var opened = s_opened.Value;
        var deposit = Public(SampleNamespace, "Account", "Deposit(decimal) : void");

        var left = opened.List.FindNode(deposit.Left!);
        var right = opened.List.FindNode(deposit.Right!);

        Assert.Same(opened.Left, left!.Assembly);
        Assert.Same(opened.Right, right!.Assembly);
    }

    [Fact]
    public void A_cancelled_comparison_stops()
    {
        var opened = s_opened.Value;
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        Assert.Throws<OperationCanceledException>(() => ApiDiff.Compare(opened.Left, opened.Right, ApiScope.PublicApi, cancelled.Token));
    }
}
