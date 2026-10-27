namespace ZGF.ILSpy.App.Tests;

public sealed class NavigationHistoryTests
{
    private static NavigationHistory Holding(params TypeDiagram[] diagrams)
    {
        var history = new NavigationHistory();
        foreach (var diagram in diagrams)
            history.PushDiagram(diagram, "diagram");
        return history;
    }

    private static List<TypeDiagram> Shown(NavigationHistory history)
    {
        return history.Entries
            .OfType<HistoryEntry.Diagram>()
            .Select(static e => e.Shown)
            .ToList();
    }

    [Fact]
    public void Working_in_the_same_diagram_again_records_it_once()
    {
        var diagram = new TypeDiagram();

        var history = Holding(diagram, diagram);

        Assert.Single(history.Entries);
    }

    [Fact]
    public void Forgetting_a_diagram_drops_each_of_its_entries_and_keeps_the_current_one_current()
    {
        var (first, second, third) = (new TypeDiagram(), new TypeDiagram(), new TypeDiagram());
        var history = Holding(first, second, first, third);

        history.Forget(first);

        Assert.Equal([second, third], Shown(history));
        Assert.Equal(1, history.CurrentIndex);
    }

    [Fact]
    public void Forgetting_the_current_diagram_makes_the_entry_before_it_current()
    {
        var (first, second) = (new TypeDiagram(), new TypeDiagram());
        var history = Holding(first, second);

        history.Forget(second);

        Assert.Equal([first], Shown(history));
        Assert.Equal(0, history.CurrentIndex);
    }

    [Fact]
    public void Forgetting_the_only_diagram_leaves_no_entry_current()
    {
        var diagram = new TypeDiagram();
        var history = Holding(diagram);

        history.Forget(diagram);

        Assert.Empty(history.Entries);
        Assert.Equal(-1, history.CurrentIndex);
    }
}
