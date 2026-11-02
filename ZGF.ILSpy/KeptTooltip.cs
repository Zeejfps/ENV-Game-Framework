using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>The tooltip a view keeps between frames: the one last looked up, with the part of the view it is
/// for, kept while the pointer crosses over to another part until that part's tooltip shows.</summary>
internal sealed class KeptTooltip
{
    private readonly record struct Kept(CodeTooltip Tooltip, int Part);

    private Kept? _kept;

    /// <summary>Keeps <paramref name="tooltip"/> for <paramref name="part"/>, unless there is none or the tooltip
    /// of another part, <paramref name="shownPart"/>, is still shown.</summary>
    public void Keep(CodeTooltip? tooltip, int part, int shownPart)
    {
        if (tooltip == null)
            return;
        if (shownPart != 0 && shownPart != part)
            return;
        _kept = new Kept(tooltip, part);
    }

    /// <summary>The tooltip kept for <paramref name="shownPart"/>, or null.</summary>
    public CodeTooltip? For(int shownPart)
    {
        if (_kept == null)
            return null;
        var kept = _kept.Value;
        if (kept.Part != shownPart)
            return null;
        return kept.Tooltip;
    }

    public void Forget() => _kept = null;
}
