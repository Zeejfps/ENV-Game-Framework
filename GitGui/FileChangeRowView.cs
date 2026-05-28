namespace GitGui;

internal static class FileChangeFormatting
{
    public static string StatusGlyph(FileChangeStatus status) => status switch
    {
        FileChangeStatus.Added => "A",
        FileChangeStatus.Modified => "M",
        FileChangeStatus.Deleted => "D",
        FileChangeStatus.Renamed => "R",
        FileChangeStatus.Copied => "C",
        FileChangeStatus.TypeChanged => "T",
        FileChangeStatus.Conflicted => "!",
        FileChangeStatus.Submodule => "S",
        _ => "·",
    };

    public static string FormatPath(FileChange file)
    {
        if (file.Status == FileChangeStatus.Renamed && !string.IsNullOrEmpty(file.OldPath))
            return $"{file.OldPath} → {file.Path}";
        if (file.PointerChange is { } pc)
            return $"{file.Path}  ·  {FormatShortSha(pc.FromSha)}..{FormatShortSha(pc.ToSha)}{FormatRangeSummary(pc)}";
        return file.Path;
    }

    private static string FormatShortSha(string sha)
    {
        if (string.IsNullOrEmpty(sha)) return "·";
        if (IsAllZeros(sha)) return "(none)";
        return sha.Length >= 7 ? sha[..7] : sha;
    }

    // Submodule isn't initialized locally → counts unavailable. Blank rather than misleading "(+0)".
    private static string FormatRangeSummary(SubmodulePointerChange pc)
    {
        if (pc.AheadCount == 0 && pc.BehindCount == 0) return string.Empty;
        if (pc.AheadCount > 0 && pc.BehindCount == 0) return $"  (+{pc.AheadCount})";
        if (pc.BehindCount > 0 && pc.AheadCount == 0) return $"  (-{pc.BehindCount})";
        return $"  (+{pc.AheadCount}/-{pc.BehindCount})";
    }

    private static bool IsAllZeros(string s)
    {
        for (var i = 0; i < s.Length; i++)
            if (s[i] != '0') return false;
        return s.Length > 0;
    }
}
