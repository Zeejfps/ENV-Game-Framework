using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Analysis;

namespace ZGF.ILSpy.Tests;

public sealed class AnalysisTests
{
    private static (AssemblyList List, BrowseNode Node) Member(string label)
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        return (list, assembly.Calculator().Child(label));
    }

    private static List<AnalysisSymbol> Run(AssemblyList list, BrowseNode node, string header) =>
        Analyzers.RunAsync(list, Analyzers.Of(node)!, Analyzers.KindsFor(Analyzers.Of(node)!).Single(k => k.Header == header))
            .ToBlockingEnumerable().SelectMany(static b => b).ToList();

    [Fact]
    public void A_method_offers_ILSpys_analyses_in_its_order()
    {
        var (_, add) = Member("Add(T) : void");

        var kinds = Analyzers.KindsFor(Analyzers.Of(add)!).Select(k => k.Header);

        Assert.Equal(["Uses", "Used By", "Implements"], kinds);
    }

    [Fact]
    public void Used_by_finds_the_callers_and_each_leads_to_its_node()
    {
        var (list, add) = Member("Add(T) : void");

        var callers = Run(list, add, "Used By");

        var plus = Assert.Single(callers);
        Assert.Equal(new SymbolShape.Member(MemberShape.Operator), plus.Shape);
        Assert.StartsWith("operator +", plus.Label);
        Assert.Equal(Trees.FixturesNamespace + ".Calculator<T> (ZGF.ILSpy.Tests)", plus.Location);
        Assert.Same(add.Parent!.Children.Single(c => c.Kind == NodeKind.Operator), list.FindNode(plus.Reference!));
    }

    [Fact]
    public void Read_by_finds_every_member_reading_a_field()
    {
        var (list, items) = Member("_items : List<T>");

        var readers = Run(list, items, "Read By").Select(r => r.Label).ToList();

        Assert.Contains(readers, l => l.StartsWith("Add("));
        Assert.Contains(readers, l => l.StartsWith("TryGet("));
        Assert.Contains(readers, l => l.StartsWith("get_Count") || l.StartsWith("Count"));
    }

    [Fact]
    public void An_interface_is_used_by_the_types_implementing_it_and_its_property_by_theirs()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var shape = assembly.Fixtures().Child("IShape");
        var area = shape.Child("Area : double");

        var users = Run(list, shape, "Used By").Select(r => r.Label);
        var implementations = Run(list, area, "Implemented By").Select(r => r.Label);
        var accessors = Analyzers.AccessorsOf(Analyzers.Of(area)!).Select(a => a.Label);

        Assert.Contains("Circle", users);
        Assert.Contains(implementations, l => l.StartsWith("Area"));
        Assert.Contains(accessors, l => l.StartsWith("get_Area"));
    }
}
