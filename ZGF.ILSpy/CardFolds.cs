using System.Text.Json.Serialization;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Diagrams;

namespace ZGF.ILSpy;

/// <summary>A part of a card that folds: its own members of the kind <see cref="Section"/>, or what it
/// inherits from the base type named <see cref="InheritedFrom"/>.</summary>
public readonly record struct CardFold(TypeAnchor Card, MemberSection? Section, string? InheritedFrom)
{
    [JsonIgnore]
    public bool IsInherited => InheritedFrom != null;

    public static CardFold Own(TypeAnchor card, MemberSection section) => new(card, section, null);

    public static CardFold Inherited(TypeAnchor card, string from) => new(card, null, from);
}

/// <summary>Which parts of a diagram's cards are open: a card's own members show until folded, what it
/// inherits stays folded until opened. <see cref="Version"/> counts the changes.</summary>
public sealed class CardFolds
{
    private readonly HashSet<CardFold> _flipped = new();

    public int Version { get; private set; }

    public bool IsOpen(CardFold fold)
    {
        var flipped = _flipped.Contains(fold);
        if (fold.IsInherited)
            return flipped;
        return !flipped;
    }

    public void Toggle(CardFold fold)
    {
        if (!_flipped.Remove(fold))
            _flipped.Add(fold);
        Version++;
    }

    /// <summary>The parts folded or opened against how they start, for the settings to keep.</summary>
    public List<CardFold> Save() => _flipped.ToList();

    /// <summary>Folds and opens the parts as <paramref name="flipped"/>, which <see cref="Save"/> gave, says.</summary>
    public void Restore(IEnumerable<CardFold> flipped)
    {
        _flipped.Clear();
        _flipped.UnionWith(flipped);
        Version++;
    }
}
