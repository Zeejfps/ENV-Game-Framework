using System.Diagnostics;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class SearchTests
{
    [Fact]
    public async Task Types_are_found_by_case_insensitive_substring()
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        var results = await Collect(list.SearchAsync("calcul", SearchKinds.Types));

        var calculator = Assert.Single(results);
        Assert.Same(assembly.Calculator(), calculator.Node);
        Assert.Equal("Calculator<T>", calculator.Text);
        Assert.Equal(Trees.FixturesNamespace, calculator.Namespace);
        Assert.Equal("ZGF.ILSpy.Tests", calculator.AssemblyName);
        Assert.Null(calculator.DeclaringType);
    }

    [Fact]
    public async Task Members_report_their_declaring_type_and_skip_accessors()
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        var tryGet = await Collect(list.SearchAsync("TRYGET", SearchKinds.Members));
        var area = await Collect(list.SearchAsync("Area", SearchKinds.Members));

        var hit = Assert.Single(tryGet);
        Assert.Same(assembly.Calculator().Child("TryGet(int, out T?) : bool"), hit.Node);
        Assert.Equal("Calculator<T>", hit.DeclaringType);
        Assert.Equal(["Circle", "IShape"], area.Select(r => r.DeclaringType).Order());
        Assert.All(area, r => Assert.Equal(NodeKind.Property, r.Node.Kind));
    }

    [Fact]
    public async Task Nested_types_and_constants_are_found()
    {
        var (list, _) = Trees.OpenTestAssembly();

        var results = await Collect(list.SearchAsync("mode"));
        var precise = await Collect(list.SearchAsync("precise"));

        Assert.Contains(results, r => r.Node.Kind == NodeKind.Enum && r.DeclaringType == "Calculator<T>");
        Assert.Contains(precise, r => r.Node.Kind == NodeKind.Constant && r.DeclaringType == "Mode");
    }

    [Fact]
    public async Task An_empty_query_finds_nothing()
    {
        var (list, _) = Trees.OpenTestAssembly();

        Assert.Empty(await Collect(list.SearchAsync("  ")));
    }

    [Fact]
    public async Task CoreLib_searches_well_under_a_second()
    {
        var list = new AssemblyList();
        var coreLib = list.Open(Trees.CoreLibPath);
        _ = coreLib.Root.Children;

        var stopwatch = Stopwatch.StartNew();
        var results = await Collect(list.SearchAsync("Dictionary"));
        stopwatch.Stop();

        Assert.Contains(results, r => r.Text == "Dictionary<TKey, TValue>" && r.Namespace == "System.Collections.Generic");
        Assert.Contains(results, r => r.Node.Kind == NodeKind.Method);
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Search took {stopwatch.ElapsedMilliseconds} ms for {results.Count} results");
    }

    [Fact]
    public async Task Results_stream_in_batches_and_stop_on_cancellation()
    {
        var list = new AssemblyList();
        list.Open(Trees.CoreLibPath);
        using var cancellation = new CancellationTokenSource();
        var batches = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var batch in list.SearchAsync("e", SearchKinds.TypesAndMembers, cancellation.Token))
            {
                Assert.NotEmpty(batch);
                batches++;
                cancellation.Cancel();
            }
        });
        Assert.Equal(1, batches);
    }

    [Fact]
    public async Task A_mode_prefix_keeps_results_to_that_kind()
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        var types = await Collect(list.SearchAsync("t:Count"));
        var properties = await Collect(list.SearchAsync("p:Count"));
        var events = await Collect(list.SearchAsync("Changed", SearchKinds.Events));

        Assert.Empty(types);
        Assert.Contains(properties, r => r.Node == assembly.Calculator().Child("Count : int"));
        Assert.All(properties, r => Assert.Equal(NodeKind.Property, r.Node.Kind));
        Assert.Equal([NodeKind.Event], events.Select(r => r.Node.Kind).Distinct());
    }

    [Fact]
    public async Task A_term_with_a_dot_matches_qualified_names()
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        var type = await Collect(list.SearchAsync("Fixtures.Calculator", SearchKinds.Types));
        var method = await Collect(list.SearchAsync("Calculator.TryGet"));

        Assert.Equal(["Calculator<T>", "Mode"], type.Select(r => r.Text).Order());
        Assert.Contains(type, r => r.Node == assembly.Calculator());
        Assert.Equal("Calculator<T>", Assert.Single(method).DeclaringType);
    }

    [Fact]
    public async Task Operators_exclude_require_whole_names_and_match_letters_in_order()
    {
        var (list, _) = Trees.OpenTestAssembly();

        var excluded = await Collect(list.SearchAsync("t:Calc -Mode"));
        var exact = await Collect(list.SearchAsync("t:=Size"));
        var fuzzy = await Collect(list.SearchAsync("t:~clcltr"));
        var regex = await Collect(list.SearchAsync("t:/^I.*e$/"));

        Assert.Equal(["Calculator<T>"], excluded.Where(r => r.Namespace == Trees.FixturesNamespace).Select(r => r.Text));
        Assert.Equal(["Size"], exact.Where(r => r.Namespace == Trees.FixturesNamespace).Select(r => r.Text));
        Assert.Contains(fuzzy, r => r.Text == "Calculator<T>");
        Assert.Contains(regex, r => r.Text == "IShape");
    }

    [Fact]
    public async Task Literals_find_constants_and_the_methods_that_load_them()
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        var number = await Collect(list.SearchAsync("c:42"));
        var text = await Collect(list.SearchAsync("\"Total: \"", SearchKinds.Literals));

        var describe = assembly.Calculator().Child("Describe() : string");
        Assert.Contains(number, r => r.Node == assembly.Calculator().Child("Answer : int"));
        Assert.Contains(number, r => r.Node == describe);
        Assert.Contains(text, r => r.Node == describe);
    }

    [Fact]
    public async Task A_metadata_token_finds_its_definition()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var token = typeof(Fixtures.Calculator<>).MetadataToken;

        var results = await Collect(list.SearchAsync($"@{token:X8}"));

        Assert.Same(assembly.Calculator(), Assert.Single(results).Node);
    }

    [Fact]
    public async Task Namespaces_assemblies_and_resources_are_found_by_name()
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        var ns = await Collect(list.SearchAsync("n:Tests.Fixtures"));
        var byName = await Collect(list.SearchAsync("a:ilspy.tests"));
        var byPath = await Collect(list.SearchAsync("af:.dll"));
        var resources = await Collect(list.SearchAsync("r:logo"));

        Assert.Same(assembly.Fixtures(), Assert.Single(ns).Node);
        Assert.Same(assembly.Root, Assert.Single(byName).Node);
        Assert.Same(assembly.Root, Assert.Single(byPath).Node);
        Assert.Equal(["logo.bmp", "logo.ico", "logo.png"], resources.Select(r => r.Text).Order());
    }

    [Fact]
    public async Task Scope_prefixes_keep_results_to_an_assembly_or_namespace()
    {
        var (list, _) = Trees.OpenTestAssembly();
        list.Open(Trees.CoreLibPath);

        var inTests = await Collect(list.SearchAsync("inassembly:ZGF.ILSpy.Tests t:Size"));
        var inFixtures = await Collect(list.SearchAsync("innamespace:Tests.Fixtures Area"));

        Assert.All(inTests, r => Assert.Equal("ZGF.ILSpy.Tests", r.AssemblyName));
        Assert.Contains(inTests, r => r.Text == "Size");
        Assert.Equal(["Circle", "IShape"], inFixtures.Select(r => r.DeclaringType).Order());
    }

    [Fact]
    public void The_query_syntax_parses_prefixes_quotes_and_scopes()
    {
        var query = SearchQuery.Parse("md:\"Get Value\" inassembly:Core innamespace:\"\" -Async");

        Assert.Equal(SearchKinds.Methods, query.Kinds);
        Assert.Equal(["Get Value", "-Async"], query.Terms);
        Assert.Equal("Core", query.InAssembly);
        Assert.Equal("", query.InNamespace);
        Assert.Equal(["System.Foo:Bar"], SearchQuery.Parse("System.Foo:Bar").Terms);
        Assert.True(SearchQuery.Parse("inassembly:Core").IsEmpty);
        Assert.Equal(255L, SearchQuery.Parse("c:0xFF").Literal());
        Assert.Equal(1.5f, SearchQuery.Parse("c:1.5f").Literal());
        Assert.Null(SearchQuery.Parse("c:abc").Literal());
    }

    [Fact]
    public async Task Kinds_combine_and_prefixes_add_up_in_place_of_the_kinds_picked()
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        var picked = await Collect(list.SearchAsync("Answer", SearchKinds.Fields | SearchKinds.Literals));
        var prefixed = SearchQuery.Parse("t:Size p:Size", SearchKinds.All);

        Assert.Contains(picked, r => r.Node == assembly.Calculator().Child("Answer : int"));
        Assert.Equal(SearchKinds.Types | SearchKinds.Properties, prefixed.Kinds);
        Assert.Equal(SearchKinds.Assemblies, SearchQuery.Parse("af:dll", SearchKinds.Types).Kinds);
        Assert.Equal(SearchKinds.Tokens, SearchQuery.Parse("@06000001").Kinds);
    }

    [Fact]
    public void Scoping_replaces_an_existing_scope_and_goes_first()
    {
        Assert.Equal("inassembly:Other Foo ", SearchQuery.WithScope("inassembly:Core Foo", "inassembly", "Other"));
        Assert.Equal("innamespace:\"\" ", SearchQuery.WithScope("", "innamespace", ""));
    }

    private static async Task<List<SearchResult>> Collect(IAsyncEnumerable<IReadOnlyList<SearchResult>> search)
    {
        var results = new List<SearchResult>();
        await foreach (var batch in search)
            results.AddRange(batch);
        return results;
    }
}
