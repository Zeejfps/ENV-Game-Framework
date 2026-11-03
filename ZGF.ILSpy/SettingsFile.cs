using System.Collections.Immutable;
using System.Diagnostics;
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
    List<AssemblyListJson> AssemblyLists,
    string? ActiveAssemblyList,
    string Theme,
    string IconTheme,
    DecompilationLanguage Language,
    DecompilerOptionsJson Decompiler,
    List<string>? FoldedOptionGroups,
    DisplayOptionsJson? Display,
    Locale? Locale,
    ApiVisibility ApiVisibility,
    SearchKinds SearchKinds,
    DockLayoutJson? Layout,
    List<BookmarkJson> Bookmarks,
    List<float>? BookmarkColumns,
    List<float>? SearchColumns,
    List<float>? ExportColumns,
    Dictionary<string, SavedFilterStateJson?>? FlagsFilters,
    WindowBoundsJson? Window,
    WindowBoundsJson? ExportWindow,
    NuGetSearchJson? NuGet,
    WindowBoundsJson? NuGetWindow)
{
    /// <summary>The one list that settings files kept before lists had names.</summary>
    public const string OldAssemblies = "Assemblies";

    /// <summary>The symbols analyzed before each list kept its own.</summary>
    public const string OldAnalyzed = "Analyzed";

    /// <summary>The one switch settings files kept before <see cref="ApiVisibility"/> had three choices.</summary>
    public const string OldShowNonPublic = "ShowNonPublic";

    public static SettingsFile From(AppSettings settings) => new(
        settings.AssemblyLists.ConvertAll(AssemblyListJson.From),
        settings.ActiveAssemblyList,
        settings.Theme.Text,
        settings.IconTheme.Text,
        settings.Language,
        DecompilerOptionsJson.From(settings.Decompiler),
        settings.FoldedOptionGroups,
        settings.Display == null ? null : DisplayOptionsJson.From(settings.Display),
        settings.Locale,
        settings.ApiVisibility,
        settings.SearchKinds,
        settings.Layout == null ? null : DockLayoutJson.From(settings.Layout),
        settings.Bookmarks.ConvertAll(BookmarkJson.From),
        settings.BookmarkColumns,
        settings.SearchColumns,
        settings.ExportColumns,
        FiltersOf(settings.FlagsFilters),
        WindowOf(settings.Window),
        WindowOf(settings.ExportWindow),
        settings.NuGet == null ? null : NuGetSearchJson.From(settings.NuGet),
        WindowOf(settings.NuGetWindow));

    private static Dictionary<string, SavedFilterStateJson?>? FiltersOf(Dictionary<string, SavedFilterState>? filters)
    {
        if (filters == null)
            return null;
        var written = new Dictionary<string, SavedFilterStateJson?>();
        foreach (var (key, filter) in filters)
            written.Add(key, SavedFilterStateJson.From(filter));
        return written;
    }

    private static WindowBoundsJson? WindowOf(WindowBounds? bounds)
    {
        if (bounds == null)
            return null;
        return WindowBoundsJson.From(bounds.Value);
    }
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
    public static AssemblyListJson From(AssemblyListRecord list) => new(
        list.Name,
        SettingsJson.PathsJsonOf(list.Assemblies),
        list.Analyzed?.Select(static CodeAnchorJson? (a) => CodeAnchorJson.From(a)).ToList(),
        PicksJsonOf(list.Pdbs),
        PicksJsonOf(list.Frameworks),
        list.Documents == null ? null : DocumentsJson.From(list.Documents));

    /// <summary>The list, without blank paths and picks or documents missing what they show; null
    /// without a name. A document that is not one at all leaves out <paramref name="field"/>.</summary>
    public AssemblyListRecord? Parse(SettingsReader file, string field)
    {
        if (Name == null)
            return null;
        return new AssemblyListRecord(Name, SettingsJson.PathsOf(Assemblies), SettingsJson.AnchorsOf(Analyzed),
            PicksOf(Pdbs), PicksOf(Frameworks), Documents?.Parse(file, field));
    }

    private static Dictionary<string, string?>? PicksJsonOf(Dictionary<AssemblyPath, string>? picks) =>
        picks?.ToDictionary(static p => p.Key.Text, static string? (p) => p.Value);

    private static Dictionary<AssemblyPath, string>? PicksOf(Dictionary<string, string?>? picks)
    {
        if (picks == null)
            return null;
        var kept = new Dictionary<AssemblyPath, string>();
        foreach (var (text, pick) in picks)
        {
            var path = AssemblyPath.Parse(text);
            if (path != null && !string.IsNullOrWhiteSpace(pick))
                kept.TryAdd(path, pick);
        }
        return kept;
    }
}

/// <summary>A symbol analyzed, as a settings file can hold it. Without a file it is none.</summary>
internal sealed record CodeAnchorJson(CodeAnchorKind Kind, string? FilePath, int Token, int ILOffset)
{
    public static CodeAnchorJson From(CodeAnchor anchor) => new(anchor.Kind, anchor.FilePath.Text, anchor.Token, anchor.ILOffset);

    public CodeAnchor? Parse()
    {
        if (FilePath == null)
            return null;
        var path = AssemblyPath.Parse(FilePath);
        if (path == null)
            return null;
        return new CodeAnchor(Kind, path, Token, ILOffset);
    }
}

/// <summary>A list's open documents as a settings file can hold them, each tab read on its own.</summary>
internal sealed record DocumentsJson(List<JsonElement>? Tabs, string? Active, string? Preview, DockLayoutJson? Layout)
{
    public static DocumentsJson From(DocumentsRecord documents)
    {
        var json = AppSettingsJson.Default.DocumentJson;
        var tabs = documents.Tabs.ConvertAll(t => JsonSerializer.SerializeToElement(DocumentJson.From(t), json));
        var layout = documents.Layout == null ? null : DockLayoutJson.From(documents.Layout);
        return new DocumentsJson(tabs, documents.Active, documents.Preview, layout);
    }

    /// <summary>The documents, without tabs missing what they show, docked as a view, or docked as
    /// a panel another tab was; null without tabs. A tab that is not one at all, or a layout missing
    /// a part or giving two parts one id, leaves out <paramref name="field"/>.</summary>
    public DocumentsRecord? Parse(SettingsReader file, string field)
    {
        if (Tabs == null)
            return null;
        var tabs = Tabs
            .Select(t => file.Parse(t, AppSettingsJson.Default.DocumentJson, field)?.Parse(() => file.LeaveOut(field)))
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
    public static DocumentJson From(DocumentRecord document) => document switch
    {
        NodeDocumentRecord node => NodeDocumentJson.From(node),
        DiagramDocumentRecord diagram => DiagramDocumentJson.From(diagram),
        ComparisonDocumentRecord comparison => ComparisonDocumentJson.From(comparison),
        _ => throw new UnreachableException(),
    };

    /// <summary>The tab; <paramref name="leaveOut"/> is told when what it shows cannot be read.</summary>
    public abstract DocumentRecord? Parse(Action leaveOut);
}

internal sealed record NodeDocumentJson(
    string? Panel,
    string? Title,
    string? FilePath,
    List<LegacyPlaceStepJson?>? Place,
    SavedPathJson? Path,
    DecompilationLanguage Language,
    CodeViewPositionJson? Position,
    GridPositionJson? GridPosition) : DocumentJson(Panel, Title)
{
    public static NodeDocumentJson From(NodeDocumentRecord node)
    {
        var code = node.View?.Code;
        var grid = node.View?.Grid;
        return new NodeDocumentJson(node.Panel, node.Title, node.Location.FilePath.Text, SettingsJson.LegacyPlaceJsonOf(node.Place),
            SavedPathJson.From(node.Location), node.Language,
            code == null ? null : CodeViewPositionJson.From(code.Value),
            grid == null ? null : GridPositionJson.From(grid.Value));
    }

    public override DocumentRecord? Parse(Action leaveOut)
    {
        if (Panel == null || FilePath == null)
            return null;
        var file = AssemblyPath.Parse(FilePath);
        if (file == null)
            return null;
        var place = SettingsJson.LegacyPlaceOf(Place);
        var location = SettingsJson.LocationOf(file, Path, place, leaveOut);
        if (location == null)
            return null;
        return new NodeDocumentRecord(Panel, Title, location, place, Language, ViewOf(Position, GridPosition));
    }

    /// <summary>The view's place as the file keeps it: a code view's, else a grid's, else none.</summary>
    private static ViewPosition? ViewOf(CodeViewPositionJson? code, GridPositionJson? grid)
    {
        if (code != null)
            return ViewPosition.OfCode(code.Value.Parse());
        if (grid != null)
            return ViewPosition.OfGrid(grid.Value.Parse());
        return null;
    }
}

/// <summary>Where a code view stood, as a settings file can hold it.</summary>
internal readonly record struct CodeViewPositionJson(int TopLine, float ScrollX, TextPositionJson Anchor, TextPositionJson Caret)
{
    public static CodeViewPositionJson From(CodeViewPosition position) =>
        new(position.TopLine, position.ScrollX, TextPositionJson.From(position.Anchor), TextPositionJson.From(position.Caret));

    public CodeViewPosition Parse() => new(TopLine, ScrollX, Anchor.Parse(), Caret.Parse());
}

/// <summary>A place in a code view's text, as a settings file can hold it.</summary>
internal readonly record struct TextPositionJson(int Line, int Column)
{
    public static TextPositionJson From(TextPosition position) => new(position.Line, position.Column);

    public TextPosition Parse() => new(Line, Column);
}

/// <summary>Where a metadata grid stood, as a settings file can hold it.</summary>
internal readonly record struct GridPositionJson(int TopRow, float ScrollX, int? Row, int? Column)
{
    public static GridPositionJson From(GridPosition position) => new(position.TopRow, position.ScrollX, position.Row, position.Column);

    public GridPosition Parse() => new(TopRow, ScrollX, Row, Column);
}

internal sealed record DiagramDocumentJson(
    string? Panel,
    string? Title,
    List<TypeAnchorJson?>? Types,
    List<string?>? HomeAssemblies,
    DiagramViewJson? View) : DocumentJson(Panel, Title)
{
    public static DiagramDocumentJson From(DiagramDocumentRecord diagram) => new(
        diagram.Panel,
        diagram.Title,
        diagram.Types.Select(static TypeAnchorJson? (t) => TypeAnchorJson.From(t)).ToList(),
        diagram.HomeAssemblies == null ? null : SettingsJson.PathsJsonOf(diagram.HomeAssemblies),
        diagram.View == null ? null : DiagramViewJson.From(diagram.View));

    public override DocumentRecord? Parse(Action leaveOut)
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
    ComparisonViewJson? View) : DocumentJson(Panel, Title)
{
    public static ComparisonDocumentJson From(ComparisonDocumentRecord comparison) => new(
        comparison.Panel,
        comparison.Title,
        comparison.LeftPath.Text,
        comparison.RightPath.Text,
        comparison.Scope,
        comparison.View == null ? null : new ComparisonViewJson(comparison.View.OnlyDifferences));

    public override DocumentRecord? Parse(Action leaveOut)
    {
        if (Panel == null || LeftPath == null || RightPath == null)
            return null;
        var left = AssemblyPath.Parse(LeftPath);
        var right = AssemblyPath.Parse(RightPath);
        if (left == null || right == null)
            return null;
        return new ComparisonDocumentRecord(Panel, Title, left, right, Scope, View?.Parse());
    }
}

/// <summary>How a comparison was viewed, as a settings file can hold it.</summary>
internal sealed record ComparisonViewJson(bool OnlyDifferences)
{
    public ComparisonViewRecord Parse() => new(OnlyDifferences);
}

/// <summary>A step down the tree as builds before versioned paths wrote it, as a settings or bookmarks file
/// can hold it. Without a kind or a label it is none.</summary>
internal sealed record LegacyPlaceStepJson(string? Kind, string? Text)
{
    public static LegacyPlaceStepJson From(LegacyPlaceStep step) => new(step.Kind, step.Text);
}

/// <summary>A place as a settings or bookmarks file can hold it, in the format of <see cref="Version"/>: a
/// step per node down from the root of the listed file. Of another version, or with a step that cannot be
/// read, it is none.</summary>
internal sealed record SavedPathJson(int Version, List<SavedPathStepJson?>? Steps)
{
    public const int CurrentVersion = 1;

    /// <summary><paramref name="place"/> as the files keep it; null for a place read from a build before
    /// versioned paths, which keeps only its <see cref="LegacyPlaceStep"/>s.</summary>
    public static SavedPathJson? From(NodePath place) => place switch
    {
        NodePath.Semantic semantic => new SavedPathJson(CurrentVersion,
            semantic.Steps.Select(static SavedPathStepJson? (s) => new SavedPathStepJson(SavedPlaces.Write(s.Segment), s.Label)).ToList()),
        NodePath.Legacy => null,
        _ => throw new UnreachableException(),
    };

    /// <summary>The path down from the root of the file at <paramref name="filePath"/>; null when it
    /// cannot be read.</summary>
    public NodePath.Semantic? Parse(AssemblyPath filePath)
    {
        if (Version != CurrentVersion || Steps == null)
            return null;
        var steps = new List<NodePathStep>(Steps.Count);
        foreach (var json in Steps)
        {
            if (json == null)
                return null;
            var step = json.Parse();
            if (step == null)
                return null;
            steps.Add(step.Value);
        }
        return NodePath.From(filePath, steps);
    }
}

/// <summary>A step of a place as a settings or bookmarks file can hold it. Without a segment that reads,
/// or a label, it is none.</summary>
internal sealed record SavedPathStepJson(string? Step, string? Label)
{
    public NodePathStep? Parse()
    {
        if (Step == null || Label == null)
            return null;
        var segment = SavedPlaces.ParseSegment(Step);
        if (segment == null)
            return null;
        return new NodePathStep(segment, Label);
    }
}

/// <summary>A type as a settings file can hold it. Without its assembly or name it is none.</summary>
internal sealed record TypeAnchorJson(string? AssemblyPath, string? FullName)
{
    public static TypeAnchorJson From(TypeAnchor anchor) => new(anchor.AssemblyPath.Text, anchor.FullName);

    public TypeAnchor? Parse()
    {
        if (AssemblyPath == null || FullName == null)
            return null;
        var path = Core.AssemblyPath.Parse(AssemblyPath);
        if (path == null)
            return null;
        return new TypeAnchor(path, FullName);
    }
}

/// <summary>How a diagram was viewed, as a settings file can hold it.</summary>
internal sealed record DiagramViewJson(float X, float Y, float Zoom, List<CardFoldJson?>? Folds, bool ShowsHiddenMembers)
{
    public static DiagramViewJson From(DiagramViewRecord view) => new(view.X, view.Y, view.Zoom,
        view.Folds?.Select(static CardFoldJson? (f) => CardFoldJson.From(f)).ToList(), view.ShowsHiddenMembers);

    public DiagramViewRecord Parse()
    {
        var folds = Folds?.Select(static f => f?.Parse()).OfType<CardFold>().ToList();
        return new DiagramViewRecord(X, Y, Zoom, folds, ShowsHiddenMembers);
    }
}

/// <summary>A card's part folded or opened, as a settings file can hold it. Without its card it is none.</summary>
internal sealed record CardFoldJson(TypeAnchorJson? Card, MemberSection? Section, string? InheritedFrom)
{
    public static CardFoldJson From(CardFold fold) => new(TypeAnchorJson.From(fold.Card), fold.Section, fold.InheritedFrom);

    public CardFold? Parse()
    {
        var card = Card?.Parse();
        if (card == null)
            return null;
        return new CardFold(card.Value, Section, InheritedFrom);
    }
}

/// <summary>The feed last searched, as a settings file can hold it. Without a feed it is none.</summary>
internal sealed record NuGetSearchJson(string? FeedUrl, bool IncludePrerelease)
{
    public static NuGetSearchJson From(SavedNuGetSearch search) => new(search.FeedUrl, search.IncludePrerelease);

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
    List<LegacyPlaceStepJson?>? Place,
    DecompilationLanguage? Language,
    SavedPathJson? Path)
{
    public static BookmarkJson From(BookmarkRecord bookmark) => new(
        bookmark.Name,
        bookmark.Enabled,
        bookmark.FileName.Text,
        bookmark.AssemblyFullName,
        bookmark.ModuleName,
        bookmark.Token,
        bookmark.Kind,
        bookmark.ILOffset,
        bookmark.LineNumber,
        bookmark.MemberName,
        bookmark.LocationNodeName,
        bookmark.ViewState == null ? null : BookmarkViewStateJson.From(bookmark.ViewState),
        SettingsJson.LegacyPlaceJsonOf(bookmark.Place),
        bookmark.Language,
        bookmark.Location == null ? null : SavedPathJson.From(bookmark.Location));

    /// <summary>The bookmark, with what the file lacks filled in; null without the absolute path of an
    /// assembly file. A place that cannot be read is no place, and <paramref name="leaveOutPlace"/> is told.</summary>
    public BookmarkRecord? Parse(Action leaveOutPlace)
    {
        if (FileName == null)
            return null;
        var path = AssemblyPath.Parse(FileName);
        if (path == null)
            return null;
        var place = SettingsJson.LegacyPlaceOf(Place);
        var location = SettingsJson.LocationOf(path, Path, place, leaveOutPlace);
        return new BookmarkRecord(Name ?? "", Enabled, path, AssemblyFullName ?? "", ModuleName ?? path.FileName,
            Token, Kind, ILOffset, LineNumber, MemberName ?? "", LocationNodeName, ViewState?.Parse(), location, place, Language);
    }
}

/// <summary>ILSpy's view of a bookmark's document, as a settings or bookmarks file can hold it.</summary>
internal sealed record BookmarkViewStateJson(
    int Version,
    int CaretOffset,
    double VerticalOffset,
    double HorizontalOffset,
    int? FoldingChecksum,
    List<BookmarkFoldingRangeJson?>? ExpandedFoldings,
    BookmarkRenderedLayoutSettingsJson? RenderedLayoutSettings,
    List<string?>? SelectedTreeNodePath)
{
    public static BookmarkViewStateJson From(BookmarkViewState view) => new(
        view.Version,
        view.CaretOffset,
        view.VerticalOffset,
        view.HorizontalOffset,
        view.FoldingChecksum,
        view.ExpandedFoldings?.Select(static BookmarkFoldingRangeJson? (f) => BookmarkFoldingRangeJson.From(f)).ToList(),
        view.RenderedLayoutSettings == null ? null : BookmarkRenderedLayoutSettingsJson.From(view.RenderedLayoutSettings),
        view.SelectedTreeNodePath == null ? null : new List<string?>(view.SelectedTreeNodePath));

    /// <summary>The view state, without missing foldings; a tree path missing a step is no path.</summary>
    public BookmarkViewState Parse()
    {
        var foldings = ExpandedFoldings?.Select(static f => f?.Parse()).OfType<BookmarkFoldingRange>().ToList();
        return new BookmarkViewState(Version, CaretOffset, VerticalOffset, HorizontalOffset, FoldingChecksum, foldings,
            RenderedLayoutSettings?.Parse(), TreePathOf(SelectedTreeNodePath));
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

/// <summary>A folded region left open, as a settings or bookmarks file can hold it.</summary>
internal sealed record BookmarkFoldingRangeJson(int Start, int End)
{
    public static BookmarkFoldingRangeJson From(BookmarkFoldingRange range) => new(range.Start, range.End);

    public BookmarkFoldingRange Parse() => new(Start, End);
}

/// <summary>ILSpy's display settings a bookmark was made in, as a settings or bookmarks file can hold them.</summary>
internal sealed record BookmarkRenderedLayoutSettingsJson(
    bool FoldBraces,
    bool ExpandXmlDocumentationComments,
    bool ExpandMemberDefinitions,
    bool ExpandUsingDeclarations,
    bool ShowDebugInfo,
    bool IndentationUseTabs,
    int IndentationSize,
    int IndentationTabSize)
{
    public static BookmarkRenderedLayoutSettingsJson From(BookmarkRenderedLayoutSettings settings) => new(
        settings.FoldBraces,
        settings.ExpandXmlDocumentationComments,
        settings.ExpandMemberDefinitions,
        settings.ExpandUsingDeclarations,
        settings.ShowDebugInfo,
        settings.IndentationUseTabs,
        settings.IndentationSize,
        settings.IndentationTabSize);

    public BookmarkRenderedLayoutSettings Parse() => new(
        FoldBraces,
        ExpandXmlDocumentationComments,
        ExpandMemberDefinitions,
        ExpandUsingDeclarations,
        ShowDebugInfo,
        IndentationUseTabs,
        IndentationSize,
        IndentationTabSize);
}

/// <summary>How the app is drawn, as a settings file can hold it. What it lacks, or holds as null, is
/// as <see cref="DisplayOptions.Default"/> has it; a font size out of range is brought within it and the
/// interface scale is the nearest offered.</summary>
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
    public static DisplayOptionsJson From(DisplayOptions options) => new(
        options.FontSize,
        options.ShowLineNumbers,
        options.HighlightCurrentLine,
        options.HighlightMatchingBrackets,
        options.ShowOmnibar,
        options.UiScale,
        options.Font == null ? null : CodeFontJson.From(options.Font),
        options.TreeTokens,
        options.ShowEmptyMetadataTables,
        options.NestedNamespaces,
        options.SystemTitleBar,
        options.PreviewTabs);

    public DisplayOptions Parse()
    {
        var d = DisplayOptions.Default;
        var options = new DisplayOptions(
            null,
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
        var scaled = options.WithUiScale(options.UiScale);
        if (FontSize == null)
            return scaled;
        return scaled.WithFontSize(FontSize.Value);
    }
}

/// <summary>A font to draw code in, as a settings file can hold it. Without its family or file it is
/// none, and the bundled font stands in.</summary>
internal sealed record CodeFontJson(string? Family, string? Path, int FaceIndex)
{
    public static CodeFontJson From(CodeFont font) => new(font.Family, font.Path, font.FaceIndex);

    public CodeFont? Parse()
    {
        if (Family == null || Path == null)
            return null;
        return new CodeFont(Family, Path, FaceIndex);
    }
}

/// <summary>The choices that change how C# is decompiled, as a settings file can hold them. What it
/// lacks, or holds as null, is as <see cref="DecompilerOptions.Default"/> has it.</summary>
internal sealed record DecompilerOptionsJson(CSharpVersion? LanguageVersion, Dictionary<string, bool>? Toggles)
{
    public static DecompilerOptionsJson From(DecompilerOptions options) =>
        new(options.LanguageVersion, new Dictionary<string, bool>(options.Toggles));

    public DecompilerOptions Parse()
    {
        var d = DecompilerOptions.Default;
        return new DecompilerOptions
        {
            LanguageVersion = LanguageVersion ?? d.LanguageVersion,
            Toggles = Toggles == null ? d.Toggles : Toggles.ToImmutableSortedDictionary(),
        };
    }
}

/// <summary>Where a window sat and how big it was, as a settings file can hold it.</summary>
internal sealed record WindowBoundsJson(int X, int Y, int Width, int Height, bool Maximized)
{
    public static WindowBoundsJson From(WindowBounds bounds) => new(bounds.X, bounds.Y, bounds.Width, bounds.Height, bounds.Maximized);

    public WindowBounds Parse() => new(X, Y, Width, Height, Maximized);
}

/// <summary>A flags filter, as a settings file can hold it. A group without values is left as it starts.</summary>
internal sealed record SavedFilterStateJson(Dictionary<string, uint[]?>? Groups, Dictionary<string, TriState>? Flags, MatchMode Mode)
{
    public static SavedFilterStateJson From(SavedFilterState filter)
    {
        var groups = filter.Groups?.ToDictionary(static g => g.Key, static uint[]? (g) => g.Value);
        return new SavedFilterStateJson(groups, filter.Flags, filter.Mode);
    }

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

/// <summary>What the shapes share.</summary>
internal static class SettingsJson
{
    public static List<string?> PathsJsonOf(List<AssemblyPath> paths) => paths.ConvertAll(static string? (p) => p.Text);

    public static List<LegacyPlaceStepJson?>? LegacyPlaceJsonOf(List<LegacyPlaceStep>? place) =>
        place?.Select(static LegacyPlaceStepJson? (s) => LegacyPlaceStepJson.From(s)).ToList();

    /// <summary>The absolute paths among <paramref name="paths"/>; none when there are none.</summary>
    public static List<AssemblyPath> PathsOf(List<string?>? paths)
    {
        if (paths == null)
            return new List<AssemblyPath>();
        return paths
            .OfType<string>()
            .Select(AssemblyPath.Parse)
            .OfType<AssemblyPath>()
            .ToList();
    }

    /// <summary>The symbols in <paramref name="anchors"/> that name a file; none when there are none.</summary>
    public static List<CodeAnchor> AnchorsOf(List<CodeAnchorJson?>? anchors)
    {
        if (anchors == null)
            return new List<CodeAnchor>();
        return anchors.Select(static a => a?.Parse()).OfType<CodeAnchor>().ToList();
    }

    /// <summary>The steps of <paramref name="place"/>, or null when it has none or a step lacks its kind or label.</summary>
    public static List<LegacyPlaceStep>? LegacyPlaceOf(List<LegacyPlaceStepJson?>? place)
    {
        if (place == null)
            return null;
        var steps = new List<LegacyPlaceStep>(place.Count);
        foreach (var step in place)
        {
            if (step == null || step.Kind == null || step.Text == null)
                return null;
            steps.Add(new LegacyPlaceStep(step.Kind, step.Text));
        }
        return steps;
    }

    /// <summary>Where a tab or bookmark in the file at <paramref name="filePath"/> was: what
    /// <paramref name="path"/> says when there is one, else what <paramref name="place"/>, as builds before
    /// versioned paths wrote it, says. Null when there is neither, or when what there is cannot be read,
    /// which <paramref name="leaveOut"/> is told; never the root for a path that cannot be read.</summary>
    public static NodePath? LocationOf(AssemblyPath filePath, SavedPathJson? path, List<LegacyPlaceStep>? place, Action leaveOut)
    {
        if (path != null)
        {
            var semantic = path.Parse(filePath);
            if (semantic == null)
                leaveOut();
            return semantic;
        }
        if (place == null)
            return null;
        var legacy = SavedPlaces.LegacyPathOf(filePath, place);
        if (legacy == null)
            leaveOut();
        return legacy;
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, Converters =
[
    typeof(EnumByName<ApiScope>), typeof(EnumByName<ApiVisibility>), typeof(EnumByName<BookmarkKind>),
    typeof(EnumByName<CodeAnchorKind>), typeof(EnumByName<CSharpVersion>), typeof(EnumByName<DecompilationLanguage>),
    typeof(EnumByName<Direction>), typeof(EnumByName<Locale>), typeof(EnumByName<MatchMode>),
    typeof(EnumByName<MemberSection>), typeof(EnumByName<SearchKinds>),
    typeof(EnumByName<TokenFormat>), typeof(EnumByName<TriState>),
])]
[JsonSerializable(typeof(SettingsFile))]
[JsonSerializable(typeof(BookmarkFileJson))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(List<JsonElement>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool?))]
[JsonSerializable(typeof(DecompilationLanguage?))]
[JsonSerializable(typeof(ApiVisibility?))]
[JsonSerializable(typeof(SearchKinds?))]
[JsonSerializable(typeof(Locale?))]
[JsonSerializable(typeof(WindowBoundsJson))]
[JsonSerializable(typeof(DecompilerOptionsJson))]
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
