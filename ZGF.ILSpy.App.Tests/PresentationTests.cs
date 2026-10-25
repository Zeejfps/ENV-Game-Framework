using ZGF.Gui.Immediate;
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
        using var temp = new TempDir();
        var copy = temp.Copy(AppStateDriver.FixturePath, Path.Combine("copy", Path.GetFileName(AppStateDriver.FixturePath)));
        var list = new AssemblyList();
        var original = list.Open(AppStateDriver.FixturePath);
        var duplicate = list.Open(copy);
        var tree = new BrowseTree();
        tree.Sync(list);

        var first = tree.Row(original.Root);
        var second = tree.Row(duplicate.Root);

        Assert.NotEqual(first.Label, second.Label);
        Assert.StartsWith(original.Name, first.Label);
        Assert.Equal($"{duplicate.Name} (copy)", second.Label);
        Assert.Equal(first.Secondary, second.Secondary);
        Assert.Equal(second.Label, tree.Label(duplicate.Root));
        Assert.Contains(original.FilePath, first.Tooltip);
        Assert.Contains(duplicate.FilePath, second.Tooltip);
        Assert.Contains(original.FullName, first.Tooltip);
    }

    [Fact]
    public void An_assembly_listed_once_reads_as_its_name_alone()
    {
        var list = new AssemblyList();
        var assembly = list.Open(AppStateDriver.FixturePath);
        var tree = new BrowseTree();
        tree.Sync(list);

        Assert.Equal(assembly.Name, tree.Row(assembly.Root).Label);
    }

    [Fact]
    public void An_assembly_row_shows_its_version_and_framework_as_muted_secondary_text()
    {
        var list = new AssemblyList();
        var assembly = list.Open(AppStateDriver.FixturePath);
        var tree = new BrowseTree();
        tree.Sync(list);

        var row = tree.Row(assembly.Root);

        Assert.Equal(assembly.Name, row.Label);
        Assert.Equal($"{assembly.Version} · net10.0", row.Secondary);
        Assert.Contains(assembly.FullName, row.Tooltip);
    }

    [Fact]
    public void Non_public_nodes_carry_an_accessibility_overlay_in_the_palette_color_and_dimmed_text()
    {
        var root = new AssemblyList().Open(AppStateDriver.FixturePath).Root;
        var ns = root.Child(AppStateDriver.FixturesNamespace);
        var privateField = ns.Child("Outline").Children.Single(c => c.Kind == NodeKind.Field && c.Accessibility == NodeAccessibility.Private);
        var tree = new BrowseTree();
        tree.SetStyle(UiTheme.Light, NodeIconPalette.Light);

        var field = tree.Row(privateField);
        var hidden = tree.Row(ns.Child("Hidden"));
        var greeter = tree.Row(ns.Child("Greeter"));

        Assert.Equal(LucideIcons.Lock, field.Icon!.Value.Overlay);
        Assert.Equal(NodeIconPalette.Light.Restricted, field.Icon!.Value.OverlayColor);
        Assert.Equal(NodeIcons.Family, field.Icon!.Value.Family);
        Assert.Equal(UiTheme.Light.TextMuted, field.LabelColor);
        Assert.Equal(LucideIcons.Shield, hidden.Icon!.Value.Overlay);
        Assert.Null(greeter.Icon!.Value.Overlay);
        Assert.Null(greeter.Secondary);
    }

    [Fact]
    public void Types_and_members_show_their_metadata_tokens_in_the_chosen_format_while_assemblies_keep_their_identity()
    {
        var list = new AssemblyList();
        var assembly = list.Open(AppStateDriver.FixturePath);
        var greeter = assembly.Root.Child(AppStateDriver.FixturesNamespace, "Greeter");
        var member = greeter.Children.First(c => c.Token != null);
        var tree = new BrowseTree();
        tree.Sync(list);
        var identity = tree.Row(assembly.Root).Secondary;

        Assert.Null(tree.Row(greeter).Secondary);
        tree.ShowTokens(TokenFormat.Hex);
        Assert.Equal($"@{greeter.Token:X8}", tree.Row(greeter).Secondary);
        Assert.StartsWith("@02", tree.Row(greeter).Secondary);
        Assert.Equal($"@{member.Token:X8}", tree.Row(member).Secondary);
        Assert.Equal(identity, tree.Row(assembly.Root).Secondary);
        tree.ShowTokens(TokenFormat.Decimal);
        Assert.Equal($"@{greeter.Token}", tree.Row(greeter).Secondary);
    }

    [Fact]
    public void A_forwarded_type_row_shows_the_forward_icon_and_the_assembly_it_is_forwarded_to()
    {
        var runtime = Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll");
        var facade = new AssemblyList().Open(runtime);
        var forwarded = facade.Root.Child("System").Children.First(c => c.Text == "Object");
        var tree = new BrowseTree();

        var row = tree.Row(forwarded);

        Assert.Equal("Object", row.Label);
        Assert.Equal(LucideIcons.Forward, row.Icon!.Value.Glyph);
        Assert.Equal("→ System.Private.CoreLib", row.Secondary);
        Assert.Equal("System.Object", row.Tooltip);
    }

    [Fact]
    public void A_non_public_type_says_its_accessibility_in_its_tooltip()
    {
        var root = new AssemblyList().Open(AppStateDriver.FixturePath).Root;
        var hidden = root.Child(AppStateDriver.FixturesNamespace, "Hidden");
        var tree = new BrowseTree();

        var row = tree.Row(hidden);

        Assert.False(hidden.IsPublicApi);
        Assert.Equal(hidden.FullName + "\ninternal", row.Tooltip);
    }

    [Fact]
    public void An_assembly_tab_is_titled_by_the_assembly_name_with_its_identity_in_the_tooltip()
    {
        var d = new AppStateDriver();
        var assembly = d.OpenFixtures();

        var tab = d.AppState.Tabs.Active!;

        Assert.Equal(assembly.Name, tab.Title);
        Assert.Contains(assembly.FullName, tab.Tooltip);
        Assert.Contains(assembly.FilePath, tab.Tooltip);
    }
}
