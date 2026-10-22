using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>The symbols analyzed in the last run, until they are found and analyzed again. Until
/// then the analyzer does not hold them, so they are what gets saved.</summary>
public sealed class LastSessionAnalyses
{
    private List<CodeAnchor>? _pending;
    private bool _started;

    public LastSessionAnalyses(List<CodeAnchor>? analyzed)
    {
        if (analyzed == null)
            return;
        if (analyzed.Count == 0)
            return;
        _pending = analyzed;
    }

    /// <summary>The symbols to find and analyze again, the first time only; null after that or when
    /// there are none.</summary>
    public List<CodeAnchor>? StartRestoring()
    {
        if (_started)
            return null;
        _started = true;
        return _pending;
    }

    public void FinishRestoring()
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
