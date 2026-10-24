using System.Collections;

namespace ZGF.ILSpy;

/// <summary>The groups folded in a panel, by key. Every change bumps <see cref="Revision"/>.</summary>
public sealed class FoldedGroups : IReadOnlyCollection<string>
{
    private readonly HashSet<string> _folded;

    public FoldedGroups(IEnumerable<string>? folded)
    {
        _folded = new HashSet<string>(folded ?? []);
    }

    public int Revision { get; private set; }

    public int Count => _folded.Count;

    public bool Contains(string group) => _folded.Contains(group);

    public void Fold(IEnumerable<string> groups)
    {
        foreach (var group in groups)
        {
            if (_folded.Add(group))
                Revision++;
        }
    }

    public void Unfold(string group)
    {
        if (_folded.Remove(group))
            Revision++;
    }

    /// <summary>Unfolds <paramref name="group"/> when it is folded, and folds it otherwise.</summary>
    public void Flip(string group)
    {
        if (!_folded.Remove(group))
            _folded.Add(group);
        Revision++;
    }

    public void UnfoldAll()
    {
        if (_folded.Count == 0)
            return;
        _folded.Clear();
        Revision++;
    }

    /// <summary>The folded groups in order, to save.</summary>
    public List<string> Save()
    {
        return _folded
            .Order()
            .ToList();
    }

    public HashSet<string>.Enumerator GetEnumerator() => _folded.GetEnumerator();

    IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
