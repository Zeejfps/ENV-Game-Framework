using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Diagrams;
using static ZGF.ILSpy.Diagrams;
using static ZGF.ILSpy.Docking;
using static ZGF.ILSpy.Navigation;

namespace ZGF.ILSpy.App.Tests;

public sealed class DiagramTests
{
    private static CodeReference TypeOf(AppStateDriver d, string type) => d.Fixture(type).Reference!;

    private static List<string> CardNames(TypeDiagram diagram)
    {
        return diagram.Cards
            .Select(static c => c.Name)
            .Order()
            .ToList();
    }

    private static TypeDiagram ShowInNew(AppStateDriver d, string type)
    {
        ShowInNewDiagram(d.AppState, TypeOf(d, type));
        d.Settle();
        return d.AppState.Tabs.Active!.Diagram!;
    }

    private static List<DocumentTab> ListDiagramTabs(AppStateDriver d)
    {
        return d.AppState.Tabs
            .Where(static t => t.Diagram != null)
            .ToList();
    }

    [Fact]
    public void Show_in_diagram_opens_a_tab_with_the_type_and_the_types_of_its_assembly_it_is_related_to()
    {
        var d = AppStateDriver.Opened();

        ShowInDiagram(d.AppState, TypeOf(d, "Vehicle"));
        d.Settle();

        var tab = d.AppState.Tabs.Active!;
        Assert.Equal("Vehicle diagram", tab.Title);
        Assert.Equal("Vehicle", tab.Diagram!.Cards[0].Name);
        Assert.Equal(["Bike", "Car", "Garage", "Vehicle"], CardNames(tab.Diagram));
        Assert.Same(tab, d.AppState.Tabs.Diagram);
        Assert.Empty(d.AppState.Errors.Entries);
    }

    [Fact]
    public void The_links_drawn_are_those_between_the_cards_shown()
    {
        var d = AppStateDriver.Opened();

        var diagram = ShowInNew(d, "Vehicle");

        Assert.Equal(4, diagram.Links.Count);
        Assert.Contains(new TypeLink(TypeOf(d, "Car"), TypeOf(d, "Vehicle"), TypeLinkKind.Inherits), diagram.Links);
        Assert.Contains(new TypeLink(TypeOf(d, "Garage"), TypeOf(d, "Vehicle"), TypeLinkKind.HoldsMany, "Vehicles"), diagram.Links);
        Assert.Contains(new TypeLink(TypeOf(d, "Vehicle"), TypeOf(d, "Garage"), TypeLinkKind.HoldsOne, "Garage"), diagram.Links);
        Assert.DoesNotContain(diagram.Links, l => l.To == TypeOf(d, "IDriven"));
    }

    [Fact]
    public void Showing_another_type_in_the_diagram_adds_it_to_the_same_tab_brought_to_the_front()
    {
        var d = AppStateDriver.Opened();
        ShowInDiagram(d.AppState, TypeOf(d, "Vehicle"));
        d.Settle();
        var diagramTab = d.AppState.Tabs.Active!;
        d.Show(d.Fixture("Greeter"));

        ShowInDiagram(d.AppState, TypeOf(d, "Counter"));
        d.Settle();

        Assert.Same(diagramTab, d.AppState.Tabs.Active);
        Assert.Single(ListDiagramTabs(d));
        Assert.Equal(["Bike", "Car", "Counter", "Garage", "Vehicle"], CardNames(diagramTab.Diagram!));
    }

    [Fact]
    public void Show_in_new_diagram_starts_another_which_types_shown_in_a_diagram_then_go_to()
    {
        var d = AppStateDriver.Opened();
        var first = ShowInNew(d, "Vehicle");

        var second = ShowInNew(d, "Greeter");
        ShowInDiagram(d.AppState, TypeOf(d, "Counter"));
        d.Settle();

        Assert.Equal(2, ListDiagramTabs(d).Count);
        Assert.Equal(["Counter", "Greeter"], CardNames(second));
        Assert.Equal(4, first.Cards.Count);
    }

    [Fact]
    public void A_new_diagram_takes_the_active_tab_while_it_shows_nothing()
    {
        var d = AppStateDriver.Opened();
        var empty = OpenNewTab(d.AppState);
        d.AppState.Tabs.MakeActive(empty);
        var tabs = d.AppState.Tabs.Count;

        ShowInNew(d, "Greeter");

        Assert.Equal(tabs, d.AppState.Tabs.Count);
        Assert.NotNull(empty.Diagram);
    }

    [Fact]
    public void A_card_grows_the_diagram_by_its_bases_interfaces_held_types_and_derived_types()
    {
        var d = AppStateDriver.Opened();
        var diagram = ShowInNew(d, "Car");
        Assert.Equal(["Car", "IDriven", "Vehicle"], CardNames(diagram));
        var vehicle = diagram.Find(TypeOf(d, "Vehicle"))!;

        ExpandHeld(d.AppState, diagram, vehicle);
        d.Settle();
        ExpandDerived(d.AppState, diagram, vehicle);
        d.Settle();

        Assert.Equal(["Bike", "Car", "Garage", "IDriven", "Vehicle"], CardNames(diagram));
        Assert.Contains(new TypeLink(TypeOf(d, "Bike"), TypeOf(d, "Vehicle"), TypeLinkKind.Inherits), diagram.Links);
        Assert.Contains(new TypeLink(TypeOf(d, "Car"), TypeOf(d, "IDriven"), TypeLinkKind.Implements), diagram.Links);
    }

    [Fact]
    public void A_base_type_of_another_assembly_is_added_only_when_asked_for()
    {
        var d = AppStateDriver.Opened();
        var diagram = ShowInNew(d, "Breakdown");
        Assert.Equal(["Breakdown"], CardNames(diagram));
        var breakdown = diagram.Cards[0];
        Assert.True(HasMissingBases(diagram, breakdown));

        ExpandBases(d.AppState, diagram, breakdown);
        d.Settle();

        Assert.Equal(["Breakdown", "Exception"], CardNames(diagram));
        Assert.False(HasMissingBases(diagram, breakdown));
        Assert.Contains(diagram.Links, static l => l.Kind == TypeLinkKind.Inherits);
        Assert.False(diagram.IsHome(diagram.Cards[1].AssemblyPath));
        Assert.True(diagram.IsHome(breakdown.AssemblyPath));
    }

    [Fact]
    public void The_implementations_of_an_interface_are_its_derived_types()
    {
        var d = AppStateDriver.Opened();
        var diagram = ShowInNew(d, "IDriven");

        ExpandDerived(d.AppState, diagram, diagram.Cards[0]);
        d.Settle();

        Assert.Equal(["Car", "IDriven"], CardNames(diagram));
        Assert.Equal([new TypeLink(TypeOf(d, "Car"), TypeOf(d, "IDriven"), TypeLinkKind.Implements)], diagram.Links);
    }

    [Fact]
    public void Removing_a_card_takes_its_links_with_it()
    {
        var d = AppStateDriver.Opened();
        var diagram = ShowInNew(d, "Vehicle");
        var version = diagram.Version;

        RemoveFromDiagram(d.AppState, diagram, TypeOf(d, "Garage"));

        Assert.Equal(["Bike", "Car", "Vehicle"], CardNames(diagram));
        Assert.DoesNotContain(diagram.Links, l => l.From == TypeOf(d, "Garage") || l.To == TypeOf(d, "Garage"));
        Assert.NotEqual(version, diagram.Version);
    }

    [Fact]
    public void A_newer_layout_supersedes_one_in_flight()
    {
        var d = AppStateDriver.Opened();
        var diagram = ShowInNew(d, "Vehicle");
        var all = diagram.Cards
            .Select(static c => new MeasuredCard(c.Type, 120f, 80f))
            .ToList();
        var first = new DiagramMeasures(all, []);
        var second = new DiagramMeasures([all[0]], []);

        LayOutDiagram(d.AppState, diagram, first);
        LayOutDiagram(d.AppState, diagram, second);
        d.Settle();

        Assert.Single(diagram.Layout!.Places);
        Assert.False(diagram.IsBusy);
    }

    [Fact]
    public void Closing_a_diagram_drops_what_it_was_reading()
    {
        var d = AppStateDriver.Opened();
        ShowInNewDiagram(d.AppState, TypeOf(d, "Vehicle"));
        var tab = d.AppState.Tabs.Active!;
        var diagram = tab.Diagram!;

        CloseTab(d.AppState, tab);
        d.Settle();

        Assert.True(diagram.IsClosed);
        Assert.Empty(diagram.Cards);
        Assert.Null(d.AppState.Tabs.Diagram);
        Assert.Empty(d.AppState.Errors.Entries);
    }

    [Fact]
    public void Going_to_code_from_a_diagram_never_replaces_it()
    {
        var d = AppStateDriver.Opened();
        d.TurnOffPreviewTabs();
        ShowInNewDiagram(d.AppState, TypeOf(d, "Vehicle"));
        d.Settle();
        var tab = d.AppState.Tabs.Active!;

        Navigate(d.AppState, TypeOf(d, "Greeter"), inNewTab: false);
        d.Settle();

        Assert.NotSame(tab, d.AppState.Tabs.Active);
        Assert.NotNull(tab.Diagram);
        Assert.Equal("Greeter", d.AppState.Tabs.Active!.Node!.Text);
    }

    [Fact]
    public void A_type_that_cannot_be_read_is_reported()
    {
        var d = AppStateDriver.Opened();
        var member = d.Fixture("Greeter").Child("Twice() : int").Reference!;

        ShowInNewDiagram(d.AppState, member);
        d.Settle();

        var error = Assert.Single(d.AppState.Errors.Entries);
        Assert.Equal("Cannot show Twice in a diagram", error.Words().Title);
    }
}
