using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The export report a tab's code view shows, in the interface's language, written again only
/// when the report or the language changes.</summary>
public sealed class ShownReport
{
    private ExportReport? _report;
    private Strings? _strings;
    private DecompiledDocument _document = DecompiledDocument.Empty;

    /// <summary><paramref name="report"/> written out with <paramref name="t"/>.</summary>
    public DecompiledDocument For(ExportReport report, Strings t)
    {
        if (ReferenceEquals(report, _report) && t == _strings)
            return _document;
        _report = report;
        _strings = t;
        _document = ExportReportText.Write(t, [report]);
        return _document;
    }
}
