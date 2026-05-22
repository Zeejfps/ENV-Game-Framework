using System.Text.RegularExpressions;

namespace GitGui;

// Lifecycle of a long-running git op as seen on the message bus. OpId is a fresh Guid the
// initiator creates so the status-bar presenter can correlate progress lines and the
// finish event back to the right row.
public readonly record struct OperationStartedMessage(Guid OpId, string Label, string Icon);

// Phase / Percent are null for output lines that aren't git's "X%" progress format
// (e.g. "From github.com:org/repo"). The raw line is always carried so the log popover
// can show everything git emitted, not just parsed progress.
public readonly record struct OperationProgressMessage(
    Guid OpId,
    string? Phase,
    float? Percent,
    string RawLine);

public readonly record struct OperationFinishedMessage(
    Guid OpId,
    bool Success,
    string? ErrorMessage);

// Parses git's --progress output. Lines look like:
//   "Counting objects: 100% (1234/1234), done."
//   "Receiving objects:  45% (556/1234), 5.1 MiB | 1.2 MiB/s"
//   "Resolving deltas:  23% (200/890)"
// Returns (phase, percent 0..1) when the line matches the standard "phase: NN%" form;
// otherwise (null, null) — the caller should still log/display the raw line.
internal static class GitProgressParser
{
    private static readonly Regex Pattern = new(
        @"^(?<phase>[A-Za-z][A-Za-z ]+?):\s*(?<pct>\d{1,3})%",
        RegexOptions.Compiled);

    public static (string? Phase, float? Percent) Parse(string line)
    {
        if (string.IsNullOrEmpty(line)) return (null, null);
        var trimmed = line.TrimStart();
        // Some lines come prefixed with "remote: " — strip it so the phase reads as the
        // server-side equivalent of the same step.
        if (trimmed.StartsWith("remote: ", StringComparison.Ordinal))
            trimmed = trimmed["remote: ".Length..];
        var match = Pattern.Match(trimmed);
        if (!match.Success) return (null, null);
        if (!int.TryParse(match.Groups["pct"].Value, out var pct)) return (null, null);
        if (pct < 0) pct = 0;
        if (pct > 100) pct = 100;
        return (match.Groups["phase"].Value.Trim(), pct / 100f);
    }
}
