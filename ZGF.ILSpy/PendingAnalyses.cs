using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>The symbols the open list had analyzed, as the app starts or the list opens, until they
/// are found and analyzed again. Until then the analyzer does not hold them, so they are what gets saved.</summary>
public sealed class PendingAnalyses
{
    private List<CodeAnchor>? _pending;

    public PendingAnalyses(IReadOnlyList<CodeAnchor> analyzed)
    {
        Expect(analyzed);
    }

    /// <summary>The symbols to find and analyze again; null when there are none.</summary>
    public List<CodeAnchor>? Pending => _pending;

    /// <summary>Waits for <paramref name="analyzed"/> to be found and analyzed again.</summary>
    public void Expect(IReadOnlyList<CodeAnchor> analyzed)
    {
        _pending = null;
        if (analyzed.Count == 0)
            return;
        _pending = analyzed.ToList();
    }

    public void Clear()
    {
        _pending = null;
    }

    public List<CodeAnchor> ListToSave(AnalyzerTree analyzer)
    {
        if (_pending != null)
            return _pending;
        return analyzer.Roots
            .Select(r => r.Symbol.Definition)
            .OfType<CodeAnchor>()
            .ToList();
    }
}
