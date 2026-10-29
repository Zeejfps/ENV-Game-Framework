using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Diagrams;

namespace ZGF.ILSpy.Tests;

public sealed class TypeCardTests
{
    private const string ZooNamespace = Trees.FixturesNamespace;

    private static readonly Lazy<(AssemblyList List, LoadedModule Assembly)> s_opened = new(Trees.OpenTestAssembly);

    private static AssemblyList List => s_opened.Value.List;

    private static CodeReference Zoo(string type) => s_opened.Value.Assembly.Root.Child(ZooNamespace, type).Reference!;

    private static TypeCard Card(string type) => TypeCards.Read(List, Zoo(type))!;

    private static List<string> NamesIn(TypeCard card, MemberSection kind)
    {
        return card.Sections
            .Single(s => s.Kind == kind).Members
            .Select(static m => m.Reference.Name)
            .ToList();
    }

    private static List<string> NamesOf(IEnumerable<TypeCard> cards)
    {
        return cards
            .Select(static c => c.Name)
            .ToList();
    }

    [Fact]
    public void A_card_names_its_type_as_the_tree_does_and_tags_what_it_is()
    {
        var animal = Card("Animal");

        Assert.Equal("Animal", animal.Name);
        Assert.Equal(ZooNamespace + ".Animal", animal.FullName);
        Assert.Equal(NodeKind.Class, animal.Kind);
        Assert.Equal(TypeStereotype.Abstract, animal.Stereotype);
        Assert.Equal(TypeStereotype.Interface, Card("IFeeds").Stereotype);
        Assert.Equal(TypeStereotype.Static, Card("Visits").Stereotype);
        Assert.Equal(TypeStereotype.None, Card("Lion").Stereotype);
        Assert.Equal(Trees.TestAssemblyPath, animal.AssemblyPath.Text);
    }

    [Fact]
    public void Its_own_members_are_listed_by_kind_in_the_tree_s_order_without_accessors_or_backing_fields()
    {
        var animal = Card("Animal");

        Assert.Equal([MemberSection.Fields, MemberSection.Properties, MemberSection.Methods], animal.Sections.Select(static s => s.Kind));
        Assert.Equal(["Tag", "_age"], NamesIn(animal, MemberSection.Fields));
        Assert.Equal(["Keeper", "Name"], NamesIn(animal, MemberSection.Properties));
        Assert.Contains("Speak", NamesIn(animal, MemberSection.Methods));
        Assert.DoesNotContain("get_Name", NamesIn(animal, MemberSection.Methods));
        Assert.Contains(Card("Keeper").Sections, static s => s.Kind == MemberSection.Events);
    }

    [Fact]
    public void A_member_reads_as_the_tree_labels_it_with_its_icon_and_accessibility()
    {
        var animal = Card("Animal");

        var age = animal.Sections[0].Members[1];
        Assert.Equal("_age : int", age.Text);
        Assert.Equal(NodeKind.Field, age.Kind);
        Assert.Equal(NodeAccessibility.Private, age.Accessibility);
        Assert.False(age.IsVisibleOutside);
        Assert.True(animal.Sections[0].Members[0].IsVisibleOutside);
    }

    [Fact]
    public void What_a_type_inherits_is_grouped_by_the_base_type_it_comes_from_without_what_it_cannot_reach()
    {
        var lion = Card("Lion");

        var group = Assert.Single(lion.Inherited);
        Assert.Equal("Animal", group.From);
        var names = group.Members
            .Select(static m => m.Reference.Name)
            .ToList();
        Assert.Equal(["Describe", "Keeper", "Mark", "Name", "Speak", "Tag"], names);
    }

    [Fact]
    public void A_type_links_to_what_it_derives_from_implements_and_holds()
    {
        var lion = Card("Lion");
        var keeper = Card("Keeper");

        Assert.Contains(new TypeLink(Zoo("Lion"), Zoo("Animal"), TypeLinkKind.Inherits), lion.Links);
        Assert.Contains(new TypeLink(Zoo("Lion"), Zoo("IFeeds"), TypeLinkKind.Implements), lion.Links);
        Assert.Equal(
        [
            new TypeLink(Zoo("Keeper"), Zoo("Animal"), TypeLinkKind.HoldsMany, "Animals"),
            new TypeLink(Zoo("Keeper"), Zoo("Enclosure"), TypeLinkKind.HoldsOne, "Home"),
        ], keeper.Links);
    }

    [Fact]
    public void Built_in_types_and_the_bases_every_type_has_are_no_links()
    {
        Assert.Equal([new TypeLink(Zoo("Animal"), Zoo("Keeper"), TypeLinkKind.HoldsOne, "Keeper")], Card("Animal").Links);
        Assert.Empty(Card("Enclosure").Links);
    }

    [Fact]
    public void Classes_and_interfaces_can_have_derived_types_and_sealed_types_cannot()
    {
        Assert.True(Card("Animal").CanHaveDerived);
        Assert.True(Card("IFeeds").CanHaveDerived);
        Assert.False(Card("Parrot").CanHaveDerived);
        Assert.False(Card("Enclosure").CanHaveDerived);
    }

    [Fact]
    public void A_type_reads_with_the_types_of_its_assembly_it_is_related_to_either_way()
    {
        var cards = TypeCards.ReadWithNeighbours(List, Zoo("Animal"), CancellationToken.None);

        Assert.Equal("Animal", cards[0].Name);
        Assert.Equal(["Keeper", "Lion", "Parrot"], NamesOf(cards.Skip(1)).Order());
    }

    [Fact]
    public void The_types_deriving_from_a_class_or_implementing_an_interface_are_found_among_the_open_assemblies()
    {
        var derived = TypeCards.ReadDerived(List, Zoo("Animal"), CancellationToken.None);
        var implementing = TypeCards.ReadDerived(List, Zoo("IFeeds"), CancellationToken.None);

        Assert.Equal(["Lion", "Parrot"], NamesOf(derived).Order());
        Assert.Equal(["Lion"], NamesOf(implementing));
    }

    [Fact]
    public void Finding_derived_types_stops_when_cancelled()
    {
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => TypeCards.ReadDerived(List, Zoo("Animal"), cancel.Token));
    }

    [Fact]
    public void A_reference_to_a_member_reads_as_no_card()
    {
        var name = s_opened.Value.Assembly.Root.Child(ZooNamespace, "Animal", "Name : string").Reference!;

        Assert.Null(TypeCards.Read(List, name));
        Assert.Empty(TypeCards.ReadWithNeighbours(List, name, CancellationToken.None));
    }
}
