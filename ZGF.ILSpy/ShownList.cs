namespace ZGF.ILSpy;

/// <summary>Which assembly list the tree last showed, by the list's generation, which changes each time
/// the list is cleared.</summary>
public sealed class ShownList
{
    private int _generation;

    /// <summary>Whether <paramref name="generation"/> differs from the one the tree last showed; records it when it does.</summary>
    public bool TakeChange(int generation)
    {
        if (generation == _generation)
            return false;
        _generation = generation;
        return true;
    }
}
