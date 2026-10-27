using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Diagrams;

namespace ZGF.ILSpy;

/// <summary>The types a diagram tab shows, as cards in the order added, the links among them, the
/// assemblies of the types the user put on it, and where its newest layout put them. A layout started for
/// it cancels the one before, whose result is then dropped; the layout it holds is the newest that
/// finished, which may not place the cards added since. Once closed, what is in flight for it is
/// cancelled and its results dropped. Mutated on the UI thread only.</summary>
public sealed class TypeDiagram
{
    private readonly List<TypeCard> _cards = new();
    private readonly HashSet<CodeReference> _types = new();
    private readonly List<TypeLink> _links = new();
    private readonly HashSet<string> _homeAssemblies = new(StringComparer.OrdinalIgnoreCase);
    private readonly CancellationTokenSource _open = new();
    private CancellationTokenSource? _layout;
    private int _reading;

    public IReadOnlyList<TypeCard> Cards => _cards;

    /// <summary>The links of the cards to one another.</summary>
    public IReadOnlyList<TypeLink> Links => _links;

    /// <summary>The newest layout that finished, or null before the first.</summary>
    public TypeLayout? Layout { get; private set; }

    /// <summary>Changes whenever a card is added or removed.</summary>
    public int Version { get; private set; }

    /// <summary>True while cards are read or laid out in the background.</summary>
    public bool IsBusy => _reading > 0 || _layout != null;

    /// <summary>Cancelled once the diagram is closed.</summary>
    public CancellationToken Token => _open.Token;

    public bool IsClosed => _open.IsCancellationRequested;

    public bool Contains(CodeReference type) => _types.Contains(type);

    /// <summary>The card of <paramref name="type"/>, or null when the diagram does not show it.</summary>
    public TypeCard? Find(CodeReference type)
    {
        foreach (var card in _cards)
        {
            if (card.Type == type)
                return card;
        }
        return null;
    }

    /// <summary>Adds the cards of <paramref name="cards"/> the diagram does not show yet, after the others.</summary>
    public void Add(IReadOnlyList<TypeCard> cards)
    {
        var shown = _cards.Count;
        foreach (var card in cards)
            AddIfNew(card);
        if (_cards.Count == shown)
            return;
        Change();
    }

    private void AddIfNew(TypeCard card)
    {
        if (!_types.Add(card.Type))
            return;
        _cards.Add(card);
    }

    public void Remove(CodeReference type)
    {
        if (!_types.Remove(type))
            return;
        _cards.RemoveAll(c => c.Type == type);
        Change();
    }

    private void Change()
    {
        Version++;
        _links.Clear();
        foreach (var card in _cards)
            AddLinksAmongCards(card);
    }

    private void AddLinksAmongCards(TypeCard card)
    {
        foreach (var link in card.Links)
        {
            if (_types.Contains(link.To))
                _links.Add(link);
        }
    }

    /// <summary>Takes the assembly of <paramref name="card"/>, which the user put on the diagram, as one of
    /// its own, whose members show even when hidden from other assemblies.</summary>
    public void TakeHome(TypeCard card)
    {
        TakeHome(card.AssemblyPath);
    }

    /// <summary>Takes the assembly at <paramref name="assemblyPath"/> as one of the diagram's own.</summary>
    public void TakeHome(string assemblyPath)
    {
        _homeAssemblies.Add(assemblyPath);
    }

    /// <summary>Whether the assembly at <paramref name="assemblyPath"/> is one of the diagram's own.</summary>
    public bool IsHome(string assemblyPath) => _homeAssemblies.Contains(assemblyPath);

    /// <summary>The files of the diagram's own assemblies.</summary>
    public IReadOnlyCollection<string> HomeAssemblies => _homeAssemblies;

    /// <summary>Counts a read of cards for the diagram as started, until <see cref="FinishReading"/>.</summary>
    public void StartReading()
    {
        _reading++;
    }

    public void FinishReading()
    {
        _reading--;
    }

    /// <summary>Starts a layout, cancelling the one in flight.</summary>
    public LayoutTicket StartLayout()
    {
        _layout?.Cancel();
        var work = CancellationTokenSource.CreateLinkedTokenSource(_open.Token);
        _layout = work;
        return new LayoutTicket(work);
    }

    /// <summary>Ends the layout <paramref name="ticket"/> was given for. False when another started since,
    /// or the diagram was closed, and what it laid out should be dropped.</summary>
    public bool FinishLayout(LayoutTicket ticket)
    {
        ticket.Work.Dispose();
        if (_layout != ticket.Work)
            return false;
        _layout = null;
        return !IsClosed;
    }

    /// <summary>Holds <paramref name="layout"/>, which the newest layout finished with.</summary>
    public void ShowLayout(TypeLayout layout)
    {
        Layout = layout;
    }

    /// <summary>Cancels what is in flight for the diagram, for good.</summary>
    public void Close()
    {
        _open.Cancel();
        _layout = null;
    }
}

/// <summary>Given for one layout of a diagram by <see cref="TypeDiagram.StartLayout"/>.</summary>
public sealed class LayoutTicket
{
    internal LayoutTicket(CancellationTokenSource work)
    {
        Work = work;
    }

    internal CancellationTokenSource Work { get; }

    public CancellationToken Token => Work.Token;
}
