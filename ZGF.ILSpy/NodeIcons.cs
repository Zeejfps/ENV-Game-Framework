using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>What a node kind's icon means, which picks its color from a <see cref="NodeIconPalette"/>.</summary>
public enum NodeIconRole
{
    Neutral,
    Type,
    Method,
    Data,
    Error,
}

/// <summary>Each node kind's pictogram in the Lucide icon font and the role that colors it.</summary>
public static class NodeIcons
{
    private readonly record struct KindIcon(string Glyph, NodeIconRole Role);

    private static readonly KindIcon[] Icons = BuildIcons();

    public const string Family = LucideIcons.Family;

    public static string Glyph(NodeKind kind) => Icons[(int)kind].Glyph;

    public static NodeIconRole Role(NodeKind kind) => Icons[(int)kind].Role;

    /// <summary>The small pictogram over a node's icon that marks it as not visible outside its
    /// assembly or type: a shield for internal, a lock for private. Null for anything wider.</summary>
    public static string? Overlay(NodeAccessibility accessibility) => accessibility switch
    {
        NodeAccessibility.Internal => LucideIcons.Shield,
        NodeAccessibility.Private or NodeAccessibility.PrivateProtected => LucideIcons.Lock,
        _ => null,
    };

    /// <summary>The icon of a node of <paramref name="kind"/> as the tree shows it: colored from
    /// <paramref name="palette"/>, with the overlay of its <paramref name="accessibility"/>.</summary>
    public static UiIcon Icon(NodeKind kind, NodeAccessibility accessibility, NodeIconPalette palette) =>
        new(Glyph(kind), Family, palette.For(kind), Overlay: Overlay(accessibility), OverlayColor: palette.Restricted);

    /// <summary>Records <paramref name="kind"/>'s <see cref="Icon"/> as the tree shows it, colored from
    /// the provided <see cref="NodeIconPalette"/>.</summary>
    public static void Draw(Ui<UiState> ui, NodeKind kind, NodeAccessibility accessibility = NodeAccessibility.None) =>
        ui.Icon(Icon(kind, accessibility, ui.State.NodeIconPalette));

    private static KindIcon[] BuildIcons()
    {
        var icons = new KindIcon[Enum.GetValues<NodeKind>().Length];
        Set(NodeKind.Assembly, LucideIcons.Package, NodeIconRole.Neutral);
        Set(NodeKind.ReferencesFolder, LucideIcons.Library, NodeIconRole.Neutral);
        Set(NodeKind.AssemblyReference, LucideIcons.Link2, NodeIconRole.Neutral);
        Set(NodeKind.ResourcesFolder, LucideIcons.Folder, NodeIconRole.Neutral);
        Set(NodeKind.Resource, LucideIcons.FileIcon, NodeIconRole.Neutral);
        Set(NodeKind.Namespace, LucideIcons.Braces, NodeIconRole.Neutral);
        Set(NodeKind.Class, LucideIcons.Box, NodeIconRole.Type);
        Set(NodeKind.Interface, LucideIcons.GitCommitHorizontal, NodeIconRole.Type);
        Set(NodeKind.Struct, LucideIcons.LayoutGrid, NodeIconRole.Type);
        Set(NodeKind.Enum, LucideIcons.ListOrdered, NodeIconRole.Type);
        Set(NodeKind.Delegate, LucideIcons.Webhook, NodeIconRole.Type);
        Set(NodeKind.Record, LucideIcons.NotebookText, NodeIconRole.Type);
        Set(NodeKind.RecordStruct, LucideIcons.NotepadText, NodeIconRole.Type);
        Set(NodeKind.BaseTypes, LucideIcons.Network, NodeIconRole.Neutral);
        Set(NodeKind.BaseType, LucideIcons.ArrowUpFromDot, NodeIconRole.Type);
        Set(NodeKind.Field, LucideIcons.Variable, NodeIconRole.Data);
        Set(NodeKind.Constant, LucideIcons.Pi, NodeIconRole.Data);
        Set(NodeKind.Property, LucideIcons.Wrench, NodeIconRole.Data);
        Set(NodeKind.Indexer, LucideIcons.Brackets, NodeIconRole.Data);
        Set(NodeKind.Event, LucideIcons.Zap, NodeIconRole.Data);
        Set(NodeKind.Method, LucideIcons.FunctionSquare, NodeIconRole.Method);
        Set(NodeKind.Constructor, LucideIcons.Hammer, NodeIconRole.Method);
        Set(NodeKind.Operator, LucideIcons.Diff, NodeIconRole.Method);
        Set(NodeKind.Error, LucideIcons.AlertCircle, NodeIconRole.Error);
        Set(NodeKind.Loading, LucideIcons.Loader2, NodeIconRole.Neutral);
        Set(NodeKind.Metadata, LucideIcons.Database, NodeIconRole.Neutral);
        Set(NodeKind.MetadataHeader, LucideIcons.FileText, NodeIconRole.Neutral);
        Set(NodeKind.MetadataDirectory, LucideIcons.List, NodeIconRole.Neutral);
        Set(NodeKind.MetadataTables, LucideIcons.Table2, NodeIconRole.Neutral);
        Set(NodeKind.MetadataTable, LucideIcons.Table, NodeIconRole.Data);
        Set(NodeKind.MetadataHeap, LucideIcons.Layers3, NodeIconRole.Data);
        Set(NodeKind.DerivedTypes, LucideIcons.GitFork, NodeIconRole.Neutral);
        Set(NodeKind.DerivedType, LucideIcons.ArrowDownToDot, NodeIconRole.Type);
        Set(NodeKind.ResourcesFile, LucideIcons.FileSpreadsheet, NodeIconRole.Neutral);
        Set(NodeKind.ResourceImage, LucideIcons.FileImage, NodeIconRole.Neutral);
        return icons;

        void Set(NodeKind kind, string glyph, NodeIconRole role) => icons[(int)kind] = new KindIcon(glyph, role);
    }
}

/// <summary>The colors node icons are drawn in, one per <see cref="NodeIconRole"/>: muted hues
/// shared with the editor's syntax colors, so color supplements the pictogram rather than
/// carrying meaning alone.</summary>
public sealed class NodeIconPalette
{
    public required uint Neutral { get; init; }
    public required uint Type { get; init; }
    public required uint Method { get; init; }
    public required uint Data { get; init; }
    public required uint Error { get; init; }

    /// <summary>The color of the accessibility overlay on non-public nodes' icons.</summary>
    public required uint Restricted { get; init; }

    public uint this[NodeIconRole role] => role switch
    {
        NodeIconRole.Type => Type,
        NodeIconRole.Method => Method,
        NodeIconRole.Data => Data,
        NodeIconRole.Error => Error,
        _ => Neutral,
    };

    public uint For(NodeKind kind) => this[NodeIcons.Role(kind)];

    public static NodeIconPalette Dark { get; } = new()
    {
        Neutral = 0xFFA6ACB8,
        Type = 0xFF88CEC3,
        Method = 0xFFD7C58F,
        Data = 0xFFB4C9E8,
        Error = 0xFFFF9A9A,
        Restricted = 0xFFE3A57A,
    };

    public static NodeIconPalette Light { get; } = new()
    {
        Neutral = 0xFF5D6675,
        Type = 0xFF146E68,
        Method = 0xFF79551D,
        Data = 0xFF365A88,
        Error = 0xFFB42332,
        Restricted = 0xFF9A4F1C,
    };

    public static NodeIconPalette GitHubDark { get; } = new()
    {
        Neutral = 0xFF8B949E,
        Type = 0xFFFFA657,
        Method = 0xFFD2A8FF,
        Data = 0xFF79C0FF,
        Error = 0xFFF85149,
        Restricted = 0xFFD29922,
    };

    public static NodeIconPalette GitHubLight { get; } = new()
    {
        Neutral = 0xFF59636E,
        Type = 0xFF953800,
        Method = 0xFF8250DF,
        Data = 0xFF0550AE,
        Error = 0xFFCF222E,
        Restricted = 0xFF9A6700,
    };

    public static NodeIconPalette SolarizedLight { get; } = new()
    {
        Neutral = 0xFF586E75,
        Type = 0xFFB58900,
        Method = 0xFF268BD2,
        Data = 0xFF6C71C4,
        Error = 0xFFDC322F,
        Restricted = 0xFFCB4B16,
    };

    public static NodeIconPalette OneDarkPro { get; } = new()
    {
        Neutral = 0xFF8B909A,
        Type = 0xFFE5C07B,
        Method = 0xFF61AFEF,
        Data = 0xFFE06C75,
        Error = 0xFFF44747,
        Restricted = 0xFFD19A66,
    };

    public static NodeIconPalette Dracula { get; } = new()
    {
        Neutral = 0xFF9CA3C4,
        Type = 0xFF8BE9FD,
        Method = 0xFF50FA7B,
        Data = 0xFFFFB86C,
        Error = 0xFFFF5555,
        Restricted = 0xFFF1FA8C,
    };

    public static NodeIconPalette Monokai { get; } = new()
    {
        Neutral = 0xFFA59F85,
        Type = 0xFF66D9EF,
        Method = 0xFFA6E22E,
        Data = 0xFFFD971F,
        Error = 0xFFF92672,
        Restricted = 0xFFE6DB74,
    };

    public static NodeIconPalette CatppuccinMocha { get; } = new()
    {
        Neutral = 0xFFA6ADC8,
        Type = 0xFFF9E2AF,
        Method = 0xFF89B4FA,
        Data = 0xFFB4BEFE,
        Error = 0xFFF38BA8,
        Restricted = 0xFFFAB387,
    };
}
