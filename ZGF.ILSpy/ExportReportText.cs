using System.Globalization;
using System.Text;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>Export reports as text: how one ended in a few words for the exports panel, and whole
/// reports, which a tab shows.</summary>
internal static class ExportReportText
{
    private const string Indent = "    ";

    /// <summary>How <paramref name="report"/>'s export ended, in a few words: when it finished, how
    /// long it took and how many problems it had.</summary>
    public static string DescribeOutcome(Strings t, ExportReport report)
    {
        if (report.Outcome == ExportOutcome.Cancelled)
            return t.ExportsCancelled;
        if (report.Outcome == ExportOutcome.Failed)
            return t.ExportsFailed;
        var finished = t.ExportsFinishedIn(FormatSeconds(report.Elapsed));
        if (report.Problems.Count == 0)
            return finished;
        return $"{finished} · {t.ExportProblemCount(report.Problems.Count)}";
    }
    private static string Summarize(Strings t, ExportReport report)
    {
        if (report.Outcome == ExportOutcome.Cancelled)
            return t.ExportSummaryCancelled(report.Name);
        if (report.Outcome == ExportOutcome.Failed)
            return t.ExportSummaryFailed(report.Name);
        var summary = t.ExportSummaryFinished(report.Name);
        if (report.Problems.Count == 0)
            return summary;
        return $"{summary} · {t.ExportProblemCount(report.Problems.Count)}";
    }

    /// <summary>Each of <paramref name="reports"/> in full: its summary, folder and time, then the
    /// files it wrote and its problems with their errors.</summary>
    public static DecompiledDocument Write(Strings t, IReadOnlyList<ExportReport> reports)
    {
        var text = new StringBuilder();
        foreach (var report in reports)
            WriteReport(text, t, report);
        return DecompiledDocument.FromText(t.ExportReportTitle, text.ToString());
    }

    private static void WriteReport(StringBuilder text, Strings t, ExportReport report)
    {
        text.AppendLine(Summarize(t, report));
        text.AppendLine(report.Folder);
        text.AppendLine(t.ExportTook(FormatSeconds(report.Elapsed)));
        text.AppendLine();
        WriteWritten(text, t, report.Written);
        WriteProblems(text, t, report.Problems);
    }

    private static string FormatSeconds(TimeSpan elapsed) => elapsed.TotalSeconds.ToString("F1", CultureInfo.CurrentCulture);

    private static void WriteWritten(StringBuilder text, Strings t, IReadOnlyList<string> written)
    {
        if (written.Count == 0)
            return;
        text.AppendLine(t.ExportWritten);
        foreach (var path in written)
            text.Append(Indent).AppendLine(path);
        text.AppendLine();
    }

    private static void WriteProblems(StringBuilder text, Strings t, IReadOnlyList<ExportProblem> problems)
    {
        if (problems.Count == 0)
            return;
        text.AppendLine(t.ExportProblemsHeading);
        foreach (var problem in problems)
            WriteProblem(text, problem);
    }

    private static void WriteProblem(StringBuilder text, ExportProblem problem)
    {
        text.Append(Indent).AppendLine(problem.Headline);
        if (problem.Details != null)
            WriteDetails(text, problem.Details);
        text.AppendLine();
    }

    private static void WriteDetails(StringBuilder text, string details)
    {
        var lines = details.Split('\n');
        foreach (var line in lines)
            text.Append(Indent).Append(Indent).AppendLine(line.TrimEnd('\r'));
    }
}
