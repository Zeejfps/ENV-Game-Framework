using System.Text.Json;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>How the real ILSpy anchors a bookmark: to a definition's metadata token, to an IL offset
/// in a method body, or to a line number of the document of a node.</summary>
public enum BookmarkKind
{
    Token,
    Body,
    Line,
}

/// <summary>A folded region left open in ILSpy's view when it made the bookmark.</summary>
public sealed record BookmarkFoldingRange(int Start, int End);

/// <summary>ILSpy's display settings that shaped the text the bookmark was made in.</summary>
public sealed record BookmarkRenderedLayoutSettings(
    bool FoldBraces,
    bool ExpandXmlDocumentationComments,
    bool ExpandMemberDefinitions,
    bool ExpandUsingDeclarations,
    bool ShowDebugInfo,
    bool IndentationUseTabs,
    int IndentationSize,
    int IndentationTabSize);

/// <summary>ILSpy's view of the document when the bookmark was made; <see cref="SelectedTreeNodePath"/>
/// is the path ILSpy's tree finds the bookmark's node by, which ILSpy needs to go to it.</summary>
public sealed record BookmarkViewState(
    int Version,
    int CaretOffset,
    double VerticalOffset,
    double HorizontalOffset,
    int? FoldingChecksum,
    List<BookmarkFoldingRange>? ExpandedFoldings,
    BookmarkRenderedLayoutSettings? RenderedLayoutSettings = null,
    List<string>? SelectedTreeNodePath = null);

/// <summary>
/// A bookmark as the real ILSpy writes it to ILSpy.Bookmarks.json, field for field, so files go
/// both ways. <see cref="LineNumber"/> counts from 1. <see cref="Place"/> and <see cref="Language"/>
/// are this app's own — the steps down its tree to the node the bookmark was made in, and that
/// document's language — which ILSpy skips; a record without a place finds one through
/// <see cref="BookmarkViewState.SelectedTreeNodePath"/>.
/// </summary>
public sealed record BookmarkRecord(
    string Name,
    bool Enabled,
    string FileName,
    string AssemblyFullName,
    string ModuleName,
    uint Token,
    BookmarkKind Kind,
    int ILOffset,
    int LineNumber,
    string MemberName,
    string? LocationNodeName,
    BookmarkViewState? ViewState,
    List<NodePathSegment>? Place = null,
    DecompilationLanguage? Language = null);

/// <summary>The bookmarks file the real ILSpy reads and writes.</summary>
public sealed record BookmarkFile(int Version, List<BookmarkRecord> Bookmarks)
{
    public const int CurrentVersion = 1;

    /// <summary>The name ILSpy gives the file, offered for a new export.</summary>
    public const string SuggestedName = "ILSpy.Bookmarks.json";

    /// <summary>Writes <paramref name="bookmarks"/> to <paramref name="path"/>, replacing the file
    /// whole. Returns why it failed, or null.</summary>
    public static string? Write(string path, List<BookmarkRecord> bookmarks)
    {
        var temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(new BookmarkFile(CurrentVersion, bookmarks), AppSettingsJson.Default.BookmarkFile));
            File.Move(temporary, path, overwrite: true);
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return $"{path}{Environment.NewLine}{e.Message}";
        }
    }

    /// <summary>The bookmarks in the file at <paramref name="path"/> that name an assembly file, as
    /// ILSpy keeps them; null with why when the file cannot be read.</summary>
    public static List<BookmarkRecord>? Read(string path, out string? error)
    {
        try
        {
            var file = JsonSerializer.Deserialize(File.ReadAllText(path), AppSettingsJson.Default.BookmarkFileJson);
            if (file is { Bookmarks: { } bookmarks })
            {
                error = null;
                return bookmarks.Select(static b => b?.Parse()).OfType<BookmarkRecord>().ToList();
            }
            error = $"{path}{Environment.NewLine}Not a bookmarks file.";
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            error = $"{path}{Environment.NewLine}{e.Message}";
            return null;
        }
    }
}
