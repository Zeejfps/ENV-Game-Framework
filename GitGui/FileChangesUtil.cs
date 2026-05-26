namespace GitGui;

/// <summary>
/// Theme-agnostic helpers for the file-changes UI. Status colors and theme-derived values
/// live on <see cref="FileChangesTokens"/>; these stayed behind because they're purely
/// data-to-data mappings (status → letter glyph, file → display path).
/// </summary>
internal static class FileChangesUtil
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
        return file.Path;
    }
}
