using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Diagrams;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Navigation;

namespace ZGF.ILSpy;

/// <summary>Diagrams of types, each in a tab of its own: showing a type on one, growing it along the
/// types' relations, taking cards off it, and laying it out in the background. Cards are read in the
/// background too, and dropped when their diagram is closed meanwhile.</summary>
public static class Diagrams
{
    /// <summary>Shows the type <paramref name="type"/> points at, with the types of its assembly it is
    /// directly related to, on the diagram the user last worked in, brought to the front; on a new one
    /// when none is open.</summary>
    public static void ShowInDiagram(AppState appState, CodeReference type)
    {
        var tab = appState.Tabs.Diagram;
        if (tab == null)
        {
            ShowInNewDiagram(appState, type);
            return;
        }
        BringToFront(appState, tab);
        RecordInHistory(appState, tab);
        AddWithNeighbours(appState, tab, type);
    }

    private static void BringToFront(AppState appState, DocumentTab tab)
    {
        LeaveActiveTab(appState);
        appState.Tabs.MakeActive(tab);
        appState.EnqueueCommand(new ViewCommand.ShowTab(tab, Beside: null));
    }

    /// <summary>Records the diagram <paramref name="tab"/> shows as worked in now, so going back returns to it.</summary>
    private static void RecordInHistory(AppState appState, DocumentTab tab)
    {
        appState.History.PushDiagram(tab.Diagram!, tab.Title!);
    }

    /// <summary>Opens the code of <paramref name="target"/>, picked on <paramref name="diagram"/>, beside it,
    /// in a new tab when <paramref name="inNewTab"/>; going back returns to the diagram.</summary>
    public static void OpenFromDiagram(AppState appState, TypeDiagram diagram, CodeReference target, bool inNewTab)
    {
        var tab = FindTabShowing(appState, diagram);
        if (tab == null)
            return;
        RecordInHistory(appState, tab);
        Navigate(appState, target, inNewTab);
    }

    /// <summary>Shows the type <paramref name="type"/> points at, with the types of its assembly it is
    /// directly related to, on a new diagram: in the active tab while it shows nothing, else in a new tab.</summary>
    public static void ShowInNewDiagram(AppState appState, CodeReference type)
    {
        LeaveActiveTab(appState);
        var tab = FindOrOpenEmptyTab(appState);
        tab.ShowDiagram(TitleOf(appState, type), new TypeDiagram());
        appState.Tabs.Keep(tab);
        appState.Tabs.MakeActive(tab);
        RecordInHistory(appState, tab);
        AddWithNeighbours(appState, tab, type);
    }

    private static DocumentTab FindOrOpenEmptyTab(AppState appState)
    {
        var empty = FindEmptyActiveTab(appState);
        if (empty == null)
            return OpenNewTab(appState);
        appState.EnqueueCommand(new ViewCommand.ShowTab(empty, Beside: null));
        return empty;
    }

    private static string TitleOf(AppState appState, CodeReference type)
    {
        var t = Strings.For(appState.Locale);
        return t.DiagramTitle(type.Name);
    }

    /// <summary>Reads the card of <paramref name="type"/> and its neighbours in the background, adds them to
    /// the diagram <paramref name="tab"/> shows, taking the type's assembly as one of its own, then
    /// commands the view to focus the card.</summary>
    private static async void AddWithNeighbours(AppState appState, DocumentTab tab, CodeReference type)
    {
        var diagram = tab.Diagram!;
        var token = diagram.Token;
        diagram.StartReading();
        var read = await Background.Run(() => TypeCards.ReadWithNeighbours(appState.Assemblies, type, token));
        diagram.FinishReading();
        if (diagram.IsClosed)
            return;
        var cards = read.Value;
        if (cards == null)
        {
            ReportUnreadable(appState, type, read.Error);
            return;
        }
        if (cards.Count == 0)
        {
            ReportUnreadable(appState, type, null);
            return;
        }
        diagram.TakeHome(cards[0]);
        diagram.Add(cards);
        appState.EnqueueCommand(new ViewCommand.FocusCard(tab, cards[0].Type));
    }

    private static void ReportUnreadable(AppState appState, CodeReference type, Exception? error)
    {
        var t = Strings.For(appState.Locale);
        var details = AppState.DescribeFailure(error, t.ErrorNoDefinition(type));
        appState.ReportError(t.ErrorShowInDiagram(type.Name), details);
    }

    /// <summary>Adds to <paramref name="diagram"/> the types <paramref name="card"/> derives from, or, for an
    /// interface, the interfaces it extends.</summary>
    public static void ExpandBases(AppState appState, TypeDiagram diagram, TypeCard card)
    {
        AddCards(appState, diagram, card, ListMissing(diagram, card, IsBaseLink));
    }

    /// <summary>Adds to <paramref name="diagram"/> the interfaces <paramref name="card"/> implements.</summary>
    public static void ExpandInterfaces(AppState appState, TypeDiagram diagram, TypeCard card)
    {
        AddCards(appState, diagram, card, ListMissing(diagram, card, IsInterfaceLink));
    }

    /// <summary>Adds to <paramref name="diagram"/> the types <paramref name="card"/>'s properties hold.</summary>
    public static void ExpandHeld(AppState appState, TypeDiagram diagram, TypeCard card)
    {
        AddCards(appState, diagram, card, ListMissing(diagram, card, IsHeldLink));
    }

    /// <summary>Whether <paramref name="card"/> derives from or extends a type <paramref name="diagram"/>
    /// does not show.</summary>
    public static bool HasMissingBases(TypeDiagram diagram, TypeCard card) => ListMissing(diagram, card, IsBaseLink).Count > 0;

    /// <summary>Whether <paramref name="card"/> implements an interface <paramref name="diagram"/> does not show.</summary>
    public static bool HasMissingInterfaces(TypeDiagram diagram, TypeCard card) => ListMissing(diagram, card, IsInterfaceLink).Count > 0;

    /// <summary>Whether <paramref name="card"/>'s properties hold a type <paramref name="diagram"/> does not show.</summary>
    public static bool HasMissingHeld(TypeDiagram diagram, TypeCard card) => ListMissing(diagram, card, IsHeldLink).Count > 0;

    /// <summary>Whether <paramref name="link"/> leads to a type derived from or extended.</summary>
    public static bool IsBaseLink(TypeLink link) => link.Kind == TypeLinkKind.Inherits;

    /// <summary>Whether <paramref name="link"/> leads to an interface implemented.</summary>
    public static bool IsInterfaceLink(TypeLink link) => link.Kind == TypeLinkKind.Implements;

    /// <summary>Whether <paramref name="link"/> leads to a type held in a property.</summary>
    public static bool IsHeldLink(TypeLink link) => link.Kind is TypeLinkKind.HoldsOne or TypeLinkKind.HoldsMany;

    /// <summary>The types <paramref name="card"/> links to as <paramref name="isKind"/> picks that
    /// <paramref name="diagram"/> does not show, each once.</summary>
    private static List<CodeReference> ListMissing(TypeDiagram diagram, TypeCard card, Func<TypeLink, bool> isKind)
    {
        return card.Links
            .Where(isKind)
            .Select(static l => l.To)
            .Where(t => !diagram.Contains(t))
            .Distinct()
            .ToList();
    }

    /// <summary>Reads the cards of <paramref name="types"/>, which <paramref name="card"/> links to, in the
    /// background and adds them to <paramref name="diagram"/>.</summary>
    private static async void AddCards(AppState appState, TypeDiagram diagram, TypeCard card, List<CodeReference> types)
    {
        if (types.Count == 0)
            return;
        diagram.StartReading();
        var read = await Background.Run(() => ReadCards(appState.Assemblies, types));
        FinishAdding(appState, diagram, card, read);
    }

    private static List<TypeCard> ReadCards(AssemblyList list, List<CodeReference> types)
    {
        return types
            .Select(t => TypeCards.Read(list, t))
            .OfType<TypeCard>()
            .ToList();
    }

    /// <summary>Searches the open assemblies in the background for the types deriving from
    /// <paramref name="card"/>, or implementing it, and adds them to <paramref name="diagram"/>.</summary>
    public static async void ExpandDerived(AppState appState, TypeDiagram diagram, TypeCard card)
    {
        var token = diagram.Token;
        diagram.StartReading();
        var read = await Background.Run(() => TypeCards.ReadDerived(appState.Assemblies, card.Type, token));
        FinishAdding(appState, diagram, card, read);
    }

    /// <summary>Adds the cards a read of the types related to <paramref name="card"/> found to
    /// <paramref name="diagram"/>, unless it was closed meanwhile; reports a read that failed.</summary>
    private static void FinishAdding(AppState appState, TypeDiagram diagram, TypeCard card, Outcome<List<TypeCard>> read)
    {
        diagram.FinishReading();
        if (diagram.IsClosed)
            return;
        if (read.Value == null)
        {
            ReportUnreadable(appState, card.Type, read.Error);
            return;
        }
        diagram.Add(read.Value);
    }

    /// <summary>Takes the card of <paramref name="type"/> off <paramref name="diagram"/>.</summary>
    public static void RemoveFromDiagram(AppState appState, TypeDiagram diagram, CodeReference type)
    {
        diagram.Remove(type);
    }

    /// <summary>Shows the diagram <paramref name="saved"/> kept in the tab <paramref name="reopened"/> waits in,
    /// the assemblies it names its own, and reads in the background the cards of its types still found,
    /// which it adds unless the tab was pointed elsewhere or the diagram closed meanwhile.</summary>
    internal static async void ReopenDiagram(AppState appState, ReopenedTab reopened, DiagramDocumentRecord saved)
    {
        if (!reopened.IsWaiting)
            return;
        var tab = reopened.Tab;
        var diagram = ShowSavedDiagram(appState, tab, saved);
        var reading = appState.Reopened.Wait(tab, saved);
        diagram.StartReading();
        var read = await Background.Run(() => ReadCards(appState.Assemblies, saved.Types));
        diagram.FinishReading();
        if (!appState.Reopened.Finish(reading))
            return;
        if (diagram.IsClosed)
            return;
        AddSavedCards(appState, diagram, read, saved);
    }

    private static TypeDiagram ShowSavedDiagram(AppState appState, DocumentTab tab, DiagramDocumentRecord saved)
    {
        var diagram = new TypeDiagram();
        TakeHomes(diagram, saved.HomeAssemblies);
        var title = saved.Title ?? string.Empty;
        tab.ShowDiagram(title, diagram);
        WorkInIfActive(appState, tab);
        return diagram;
    }

    private static void TakeHomes(TypeDiagram diagram, List<string>? assemblies)
    {
        if (assemblies == null)
            return;
        foreach (var assembly in assemblies)
            diagram.TakeHome(assembly);
    }

    /// <summary>Has the diagram <paramref name="tab"/> shows taken as the one the user works in, when the tab is the active one.</summary>
    private static void WorkInIfActive(AppState appState, DocumentTab tab)
    {
        if (tab != appState.Tabs.Active)
            return;
        appState.Tabs.MakeActive(tab);
    }

    private static List<TypeCard> ReadCards(AssemblyList list, List<TypeAnchor> types)
    {
        return types
            .Select(t => TypeCards.Read(list, t))
            .OfType<TypeCard>()
            .ToList();
    }

    private static void AddSavedCards(AppState appState, TypeDiagram diagram, Outcome<List<TypeCard>> read, DiagramDocumentRecord saved)
    {
        if (read.Value == null)
        {
            ReportUnreadableDiagram(appState, saved, read.Error);
            return;
        }
        diagram.Add(read.Value);
    }

    private static void ReportUnreadableDiagram(AppState appState, DiagramDocumentRecord saved, Exception? error)
    {
        var t = Strings.For(appState.Locale);
        var title = saved.Title ?? string.Empty;
        appState.ReportError(t.ErrorShowInDiagram(title), AppState.DescribeFailure(error, ""));
    }

    /// <summary>Lays out <paramref name="diagram"/> in the background, its cards and labels as large as
    /// <paramref name="measures"/> says, cancelling the layout in flight, and holds the layout unless
    /// another was started meanwhile or the diagram was closed.</summary>
    public static async void LayOutDiagram(AppState appState, TypeDiagram diagram, DiagramMeasures measures)
    {
        if (diagram.IsClosed)
            return;
        var ticket = diagram.StartLayout();
        var laidOut = await Background.Run(() => TypeLayout.Arrange(measures, ticket.Token));
        if (!diagram.FinishLayout(ticket))
            return;
        if (laidOut.Value == null)
        {
            ReportLayoutFailure(appState, laidOut.Error);
            return;
        }
        diagram.ShowLayout(laidOut.Value);
    }

    private static void ReportLayoutFailure(AppState appState, Exception? error)
    {
        var t = Strings.For(appState.Locale);
        appState.ReportError(t.ErrorLayOutDiagram, AppState.DescribeFailure(error, ""));
    }
}
