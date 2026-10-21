using System.Diagnostics;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class SearchTests
{
    [Fact]
    public async Task Types_are_found_by_case_insensitive_substring()
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        var results = await Collect(list.SearchAsync("calcul", SearchTargets.Types));

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

        var tryGet = await Collect(list.SearchAsync("TRYGET", SearchTargets.Members));
        var area = await Collect(list.SearchAsync("Area", SearchTargets.Members));

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
            await foreach (var batch in list.SearchAsync("e", SearchTargets.All, cancellation.Token))
            {
                Assert.NotEmpty(batch);
                batches++;
                cancellation.Cancel();
            }
        });
        Assert.Equal(1, batches);
    }

    private static async Task<List<SearchResult>> Collect(IAsyncEnumerable<IReadOnlyList<SearchResult>> search)
    {
        var results = new List<SearchResult>();
        await foreach (var batch in search)
            results.AddRange(batch);
        return results;
    }
}
