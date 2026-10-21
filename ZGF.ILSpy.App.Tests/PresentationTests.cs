using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.App.Tests;

public sealed class PresentationTests
{
    [Fact]
    public void Every_node_kind_has_its_own_icon_font_glyph_and_a_colored_role_in_both_palettes()
    {
        var kinds = Enum.GetValues<NodeKind>();

        foreach (var kind in kinds)
        {
            var glyph = NodeIcons.Glyph(kind);
            Assert.Equal(1, glyph.Length);
            Assert.InRange((int)glyph[0], 0xE000, 0xF8FF);
            Assert.NotEqual(0u, NodeIconPalette.Dark.For(kind));
            Assert.NotEqual(0u, NodeIconPalette.Light.For(kind));
        }
        Assert.Equal(kinds.Length, kinds.Select(NodeIcons.Glyph).Distinct().Count());
    }

    [Fact]
    public void Two_copies_of_one_assembly_read_apart_by_folder_and_carry_their_files_in_their_tooltips()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"ilspy-{Guid.NewGuid():N}", "copy");
        Directory.CreateDirectory(folder);
        var copy = Path.Combine(folder, Path.GetFileName(WorkspaceDriver.FixturePath));
        File.Copy(WorkspaceDriver.FixturePath, copy);
        var list = new AssemblyList();
        try
        {
            var original = list.Open(WorkspaceDriver.FixturePath);
            var duplicate = list.Open(copy);
            var tree = new BrowseTree(static _ => { });
            tree.Sync(list);

            var first = tree.Row(original.Root);
            var second = tree.Row(duplicate.Root);

            Assert.NotEqual(first.Label, second.Label);
            Assert.StartsWith(original.Name, first.Label);
            Assert.Equal($"{duplicate.Name} (copy)", second.Label);
            Assert.Contains(original.FilePath, first.Tooltip);
            Assert.Contains(duplicate.FilePath, second.Tooltip);
            Assert.Contains(original.FullName, first.Tooltip);
        }
        finally
        {
            list.Clear();
            try
            {
                Directory.Delete(Path.GetDirectoryName(folder)!, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    [Fact]
    public void An_assembly_listed_once_reads_as_its_name_alone()
    {
        var list = new AssemblyList();
        var assembly = list.Open(WorkspaceDriver.FixturePath);
        var tree = new BrowseTree(static _ => { });
        tree.Sync(list);

        Assert.Equal(assembly.Name, tree.Row(assembly.Root).Label);
    }

    [Fact]
    public void A_non_public_type_says_its_accessibility_in_its_tooltip()
    {
        var root = new AssemblyList().Open(WorkspaceDriver.FixturePath).Root;
        var hidden = root.Children.Single(c => c.Text == WorkspaceDriver.FixturesNamespace).Children.Single(c => c.Text == "Hidden");
        var tree = new BrowseTree(static _ => { });

        var row = tree.Row(hidden);

        Assert.False(hidden.IsPublicApi);
        Assert.Equal(hidden.FullName + "\ninternal", row.Tooltip);
    }

    [Fact]
    public void An_assembly_tab_is_titled_by_the_assembly_name_with_its_identity_in_the_tooltip()
    {
        var d = new WorkspaceDriver();
        var assembly = d.OpenFixtures();

        var tab = d.Workspace.ActiveTab!;

        Assert.Equal(assembly.Name, tab.Title);
        Assert.Contains(assembly.FullName, tab.Tooltip);
        Assert.Contains(assembly.FilePath, tab.Tooltip);
    }
}
