using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy.Tests;

public sealed class BlobTooltipTests
{
    private static MetadataGrid Grid(string table)
    {
        var node = Trees.TestAssembly.Root.Child("Metadata", "Tables").Table(table);
        return Assert.IsType<GridContent>(node.Content()).Grid;
    }

    private static string? TooltipText(MetadataGrid grid, int row, string column) => grid.Tooltip(row, grid.Column(column))?.Text;

    private static List<string?> ListTooltipTexts(MetadataGrid grid, string column)
    {
        return Enumerable.Range(0, grid.RowCount)
            .Select(row => TooltipText(grid, row, column))
            .ToList();
    }

    [Fact]
    public void A_property_signature_reads_as_IL_declares_the_property_with_its_index_parameters()
    {
        var properties = Grid("Property");

        var count = TooltipText(properties, properties.RowNamed("Count"), "Signature");
        var indexer = TooltipText(properties, properties.RowNamed("Item"), "Signature");

        Assert.Equal("instance int32 Count()", count);
        Assert.Equal("instance !T Item(int32)", indexer);
    }

    [Fact]
    public void A_signature_of_locals_lists_each_local_by_its_index()
    {
        var signatures = ListTooltipTexts(Grid("StandAloneSig"), "Signature");

        Assert.Contains(signatures, s => s != null && s.StartsWith("[0] ", StringComparison.Ordinal));
        Assert.DoesNotContain(signatures, s => s != null && s.Contains("/* signature", StringComparison.Ordinal));
    }

    [Fact]
    public void A_constant_reads_as_the_literal_it_holds()
    {
        var values = ListTooltipTexts(Grid("Constant"), "Value");

        Assert.Contains("42", values);
    }
}
