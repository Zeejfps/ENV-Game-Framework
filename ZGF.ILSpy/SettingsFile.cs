using System.Text.Json;
using System.Text.Json.Serialization;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Comparison;
using ZGF.ILSpy.Core.Diagrams;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The settings file as it is written: what <see cref="AppSettings"/> keeps, under the names
/// the file has it by. It is only ever written; <see cref="SettingsReader"/> reads a file one field at
/// a time, through the shapes below, which take what a hand-edited or older file can hold.</summary>
internal sealed record SettingsFile(
    List<AssemblyListRecord> AssemblyLists,
    string? ActiveAssemblyList,
    string Theme,
    string IconTheme,
    DecompilationLanguage Language,
    DecompilerOptions Decompiler,
    List<string>? FoldedOptionGroups,
    DisplayOptions? Display,
    Locale? Locale,
    ApiVisibility ApiVisibility,
    SearchKinds SearchKinds,
    DockLayout? Layout,
    List<BookmarkRecord> Bookmarks,
    List<float>? BookmarkColumns,
    List<float>? SearchColumns,
    List<float>? ExportColumns,
    Dictionary<string, SavedFilterState>? FlagsFilters,
    WindowBounds? Window,
    WindowBounds? ExportWindow,
    SavedNuGetSearch? NuGet,
    WindowBounds? NuGetWindow)
{
    /// <summary>The one list that settings files kept before lists had names.</summary>
    public const string OldAssemblies = "Assemblies";

    /// <summary>The symbols analyzed before each list kept its own.</summary>
    public const string OldAnalyzed = "Analyzed";

    /// <summary>The one switch settings files kept before <see cref="ApiVisibility"/> had three choices.</summary>
    public const string OldShowNonPublic = "ShowNonPublic";

    public static SettingsFile Of(AppSettings settings) => new(
        settings.AssemblyLists,
        settings.ActiveAssemblyList,
        settings.Theme,
        settings.IconTheme,
        settings.Language,
        settings.Decompiler,
        settings.FoldedOptionGroups,
        settings.Display,
        settings.Locale,
        settings.ApiVisibility,
        settings.SearchKinds,
        settings.Layout,
        settings.Bookmarks,
        settings.BookmarkColumns,
        settings.SearchColumns,
        settings.ExportColumns,
        settings.FlagsFilters,
        settings.Window,
        settings.ExportWindow,
        settings.NuGet,
        settings.NuGetWindow);
}

/// <summary>An assembly list as a settings file can hold it. Without a name it is no list.</summary>
internal sealed record AssemblyListJson(
    string? Name,
    List<string?>? Assemblies,
    List<CodeAnchorJson?>? Analyzed,
    Dictionary<string, string?>? Pdbs,
    Dictionary<string, string?>? Frameworks,
    DocumentsJson? Documents)
{
    /// <summary>The list, without blank paths and picks or documents missing what they show; null
    /// without a name. A document that is not one at all leaves out <paramref name="field"/>.</summary>
    public AssemblyListRecord? Parse(SettingsReader file, string field)
    {
        if (Name == null)
            return null;
        return new AssemblyListRecord(Name, SettingsJson.PathsOf(Assemblies), SettingsJson.AnchorsOf(Analyzed),
            PicksOf(Pdbs), PicksOf(Frameworks), Documents?.Parse(file, field));
    }

    private static Dictionary<string, string>? PicksOf(Dictionary<string, string?>? picks)
    {
        if (picks == null)
            return null;
        var kept = new Dictionary<string, string>();
        foreach (var (path, pick) in picks)
        {
            if (!string.IsNullOrWhiteSpace(pick))
                kept.Add(path, pick);
        }
        return kept;
    }
}

/// <summary>A symbol analyzed, as a settings file can hold it. Without a file it is none.</summary>
internal sealed record CodeAnchorJson(CodeAnchorKind Kind, string? FilePath, int Token, int ILOffset)
{
    public CodeAnchor? Parse()
    {
        if (FilePath == null)
            return null;
        return new CodeAnchor(Kind, FilePath, Token, ILOffset);
    }
}

/// <summary>A list's open documents as a settings file can hold them, each tab read on its own.</summary>
internal sealed record DocumentsJson(List<JsonElement>? Tabs, string? Active, string? Preview, DockLayoutJson? Layout)
{
    /// <summary>The documents, without tabs missing what they show, docked as a view, or docked as
    /// a panel another tab was; null without tabs. A tab that is not one at all, or a layout missing
    /// a part or giving two parts one id, leaves out <paramref name="field"/>.</summary>
    public DocumentsRecord? Parse(SettingsReader file, string field)
    {
        if (Tabs == null)
            return null;
        var tabs = Tabs
            .Select(t => file.Parse(t, AppSettingsJson.Default.DocumentJson, field)?.Parse())
            .OfType<DocumentRecord>()
            .Where(static t => !DockPanels.IsView(t.Panel))
            .DistinctBy(static t => t.Panel)
            .ToList();
        return new DocumentsRecord(tabs, Active, Preview, ParseLayout(file, field));
    }

    private DockLayout? ParseLayout(SettingsReader file, string field)
    {
        if (Layout == null)
            return null;
        var layout = Layout.Parse();
        if (layout != null && !layout.RepeatsAnId())
            return layout;
        file.LeaveOut(field);
        return null;
    }
}

/// <summary>An open tab as a settings file can hold it. Without a panel or what it shows it is none.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(NodeDocumentJson), "node")]
[JsonDerivedType(typeof(DiagramDocumentJson), "diagram")]
[JsonDerivedType(typeof(ComparisonDocumentJson), "comparison")]
internal abstract record DocumentJson(string? Panel, string? Title)
{
    public abstract DocumentRecord? Parse();
}

internal sealed record NodeDocumentJson(
    string? Panel,
    string? Title,
    string? FilePath,
    List<NodePathSegmentJson?>? Place,
    DecompilationLanguage Language,
    CodeViewPosition? Position) : DocumentJson(Panel, Title)
{
    public override DocumentRecord? Parse()
    {
        if (Panel == null || FilePath == null)
            return null;
        var place = SettingsJson.SegmentsOf(Place);
        if (place == null)
            return null;
        return new NodeDocumentRecord(Panel, Title, FilePath, place, Language, Position);
    }
}

internal sealed record DiagramDocumentJson(
    string? Panel,
    string? Title,
    List<TypeAnchorJson?>? Types,
    List<string?>? HomeAssemblies,
    DiagramViewJson? View) : DocumentJson(Panel, Title)
{
    public override DocumentRecord? Parse()
    {
        if (Panel == null || Types == null)
            return null;
        var types = Types.Select(static t => t?.Parse()).OfType<TypeAnchor>().ToList();
        var homes = HomeAssemblies == null ? null : SettingsJson.PathsOf(HomeAssemblies);
        return new DiagramDocumentRecord(Panel, Title, types, homes, View?.Parse());
    }
}

internal sealed record ComparisonDocumentJson(
    string? Panel,
    string? Title,
    string? LeftPath,
    string? RightPath,
    ApiScope Scope,
    ComparisonViewRecord? View) : DocumentJson(Panel, Title)
{
    public override DocumentRecord? Parse()
    {
        if (Panel == null || LeftPath == null || RightPath == null)
            return null;
        return new ComparisonDocumentRecord(Panel, Title, LeftPath, RightPath, Scope, View);
    }
}

/// <summary>A step down the tree as a settings or bookmarks file can hold it. Without a label it is none.</summary>
internal sealed record NodePathSegmentJson(NodeKind Kind, string? Text);

/// <summary>A type as a settings file can hold it. Without its assembly or name it is none.</summary>
internal sealed record TypeAnchorJson(string? AssemblyPath, string? FullName)
{
    public TypeAnchor? Parse()
    {
        if (AssemblyPath == null || FullName == null)
            return null;
        return new TypeAnchor(AssemblyPath, FullName);
    }
}

/// <summary>How a diagram was viewed, as a settings file can hold it.</summary>
internal sealed record DiagramViewJson(float X, float Y, float Zoom, List<CardFoldJson?>? Folds, bool ShowsHiddenMembers)
{
    public DiagramViewRecord Parse()
    {
        var folds = Folds?.Select(static f => f?.Parse()).OfType<CardFold>().ToList();
        return new DiagramViewRecord(X, Y, Zoom, folds, ShowsHiddenMembers);
    }
}

/// <summary>A card's part folded or opened, as a settings file can hold it. Without its card it is none.</summary>
internal sealed record CardFoldJson(TypeAnchorJson? Card, MemberSection? Section, string? InheritedFrom)
{
    public CardFold? Parse()
    {
        if (Card?.Parse() is not { } card)
            return null;
        return new CardFold(card, Section, InheritedFrom);
    }
}

/// <summary>The feed last searched, as a settings file can hold it. Without a feed it is none.</summary>
internal sealed record NuGetSearchJson(string? FeedUrl, bool IncludePrerelease)
{
    public SavedNuGetSearch? Parse()
    {
        if (FeedUrl == null)
            return null;
        return new SavedNuGetSearch(FeedUrl, IncludePrerelease);
    }
}

/// <summary>A bookmark as this app's settings or the real ILSpy's bookmarks file can hold it.</summary>
internal sealed record BookmarkJson(
    string? Name,
    bool Enabled,
    string? FileName,
    string? AssemblyFullName,
    string? ModuleName,
    uint Token,
    BookmarkKind Kind,
    int ILOffset,
    int LineNumber,
    string? MemberName,
    string? LocationNodeName,
    BookmarkViewStateJson? ViewState,
    List<NodePathSegmentJson?>? Place,
    DecompilationLanguage? Language)
{
    /// <summary>The bookmark, with what the file lacks filled in; null without an assembly file. A
    /// place missing a step's label is no place.</summary>
    public BookmarkRecord? Parse()
    {
        if (string.IsNullOrEmpty(FileName))
            return null;
        return new BookmarkRecord(Name ?? "", Enabled, FileName, AssemblyFullName ?? "", ModuleName ?? Path.GetFileName(FileName),
            Token, Kind, ILOffset, LineNumber, MemberName ?? "", LocationNodeName, ViewState?.Parse(), SettingsJson.SegmentsOf(Place), Language);
    }
}

/// <summary>ILSpy's view of a bookmark's document, as a settings or bookmarks file can hold it.</summary>
internal sealed record BookmarkViewStateJson(
    int Version,
    int CaretOffset,
    double VerticalOffset,
    double HorizontalOffset,
    int? FoldingChecksum,
    List<BookmarkFoldingRange?>? ExpandedFoldings,
    BookmarkRenderedLayoutSettings? RenderedLayoutSettings,
    List<string?>? SelectedTreeNodePath)
{
    /// <summary>The view state, without missing foldings; a tree path missing a step is no path.</summary>
    public BookmarkViewState Parse()
    {
        var foldings = ExpandedFoldings?.OfType<BookmarkFoldingRange>().ToList();
        return new BookmarkViewState(Version, CaretOffset, VerticalOffset, HorizontalOffset, FoldingChecksum, foldings,
            RenderedLayoutSettings, TreePathOf(SelectedTreeNodePath));
    }

    private static List<string>? TreePathOf(List<string?>? path)
    {
        if (path == null)
            return null;
        var steps = path.OfType<string>().ToList();
        if (steps.Count != path.Count)
            return null;
        return steps;
    }
}

/// <summary>How the app is drawn, as a settings file can hold it. What it lacks, or holds as null, is
/// as <see cref="DisplayOptions.Default"/> has it.</summary>
internal sealed record DisplayOptionsJson(
    float? FontSize,
    bool? ShowLineNumbers,
    bool? HighlightCurrentLine,
    bool? HighlightMatchingBrackets,
    bool? ShowOmnibar,
    float? UiScale,
    CodeFontJson? Font,
    TokenFormat? TreeTokens,
    bool? ShowEmptyMetadataTables,
    bool? NestedNamespaces,
    bool? SystemTitleBar,
    bool? PreviewTabs)
{
    public DisplayOptions Parse()
    {
        var d = DisplayOptions.Default;
        return new DisplayOptions(
            FontSize,
            ShowLineNumbers ?? d.ShowLineNumbers,
            HighlightCurrentLine ?? d.HighlightCurrentLine,
            HighlightMatchingBrackets ?? d.HighlightMatchingBrackets,
            ShowOmnibar ?? d.ShowOmnibar,
            UiScale ?? d.UiScale,
            Font?.Parse(),
            TreeTokens ?? d.TreeTokens,
            ShowEmptyMetadataTables ?? d.ShowEmptyMetadataTables,
            NestedNamespaces ?? d.NestedNamespaces,
            SystemTitleBar ?? d.SystemTitleBar,
            PreviewTabs ?? d.PreviewTabs);
    }
}

/// <summary>A font to draw code in, as a settings file can hold it. Without its family or file it is
/// none, and the bundled font stands in.</summary>
internal sealed record CodeFontJson(string? Family, string? Path, int FaceIndex)
{
    public CodeFont? Parse()
    {
        if (Family == null || Path == null)
            return null;
        return new CodeFont(Family, Path, FaceIndex);
    }
}

/// <summary>A flags filter, as a settings file can hold it. A group without values is left as it starts.</summary>
internal sealed record SavedFilterStateJson(Dictionary<string, uint[]?>? Groups, Dictionary<string, TriState>? Flags, MatchMode Mode)
{
    public SavedFilterState Parse()
    {
        return new SavedFilterState(GroupsOf(Groups), Flags, Mode);
    }

    private static Dictionary<string, uint[]>? GroupsOf(Dictionary<string, uint[]?>? groups)
    {
        if (groups == null)
            return null;
        var kept = new Dictionary<string, uint[]>();
        foreach (var (group, values) in groups)
        {
            if (values != null)
                kept.Add(group, values);
        }
        return kept;
    }
}

/// <summary>The real ILSpy's bookmarks file as it can hold them, each bookmark read on its own.</summary>
internal sealed record BookmarkFileJson(int Version, List<JsonElement>? Bookmarks);

/// <summary>What the read shapes share.</summary>
internal static class SettingsJson
{
    /// <summary><paramref name="paths"/> without blank ones; none when there are none.</summary>
    public static List<string> PathsOf(List<string?>? paths)
    {
        if (paths == null)
            return new List<string>();
        return paths.Where(static p => !string.IsNullOrWhiteSpace(p)).OfType<string>().ToList();
    }

    /// <summary>The symbols in <paramref name="anchors"/> that name a file; none when there are none.</summary>
    public static List<CodeAnchor> AnchorsOf(List<CodeAnchorJson?>? anchors)
    {
        if (anchors == null)
            return new List<CodeAnchor>();
        return anchors.Select(static a => a?.Parse()).OfType<CodeAnchor>().ToList();
    }

    /// <summary>The steps of <paramref name="place"/>, or null when it has none or a step lacks its label.</summary>
    public static List<NodePathSegment>? SegmentsOf(List<NodePathSegmentJson?>? place)
    {
        if (place == null)
            return null;
        var segments = new List<NodePathSegment>(place.Count);
        foreach (var segment in place)
        {
            if (segment?.Text is not { } text)
                return null;
            segments.Add(new NodePathSegment(segment.Kind, text));
        }
        return segments;
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, Converters =
[
    typeof(EnumByName<ApiScope>), typeof(EnumByName<ApiVisibility>), typeof(EnumByName<BookmarkKind>),
    typeof(EnumByName<CodeAnchorKind>), typeof(EnumByName<CSharpVersion>), typeof(EnumByName<DecompilationLanguage>),
    typeof(EnumByName<Direction>), typeof(EnumByName<Locale>), typeof(EnumByName<MatchMode>),
    typeof(EnumByName<MemberSection>), typeof(EnumByName<NodeKind>), typeof(EnumByName<SearchKinds>),
    typeof(EnumByName<TokenFormat>), typeof(EnumByName<TriState>),
])]
[JsonSerializable(typeof(SettingsFile))]
[JsonSerializable(typeof(BookmarkFile))]
[JsonSerializable(typeof(BookmarkFileJson))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(List<JsonElement>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool?))]
[JsonSerializable(typeof(DecompilationLanguage?))]
[JsonSerializable(typeof(ApiVisibility?))]
[JsonSerializable(typeof(SearchKinds?))]
[JsonSerializable(typeof(Locale?))]
[JsonSerializable(typeof(WindowBounds?))]
[JsonSerializable(typeof(DecompilerOptions))]
[JsonSerializable(typeof(DisplayOptionsJson))]
[JsonSerializable(typeof(DockLayoutJson))]
[JsonSerializable(typeof(List<string?>))]
[JsonSerializable(typeof(List<float>))]
[JsonSerializable(typeof(List<CodeAnchorJson?>))]
[JsonSerializable(typeof(Dictionary<string, SavedFilterStateJson?>))]
[JsonSerializable(typeof(NuGetSearchJson))]
[JsonSerializable(typeof(AssemblyListJson))]
[JsonSerializable(typeof(DocumentJson))]
[JsonSerializable(typeof(BookmarkJson))]
internal sealed partial class AppSettingsJson : JsonSerializerContext;
