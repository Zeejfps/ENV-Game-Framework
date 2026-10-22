using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>The code view's editor colors: syntax by role, mapped to each <see cref="SpanKind"/>,
/// and the gutter, fold chip and highlight states. Provided per theme with <c>ui.Provide</c>.</summary>
public sealed class CodeViewPalette
{
    public required uint Plain { get; init; }
    public required uint Keyword { get; init; }
    public required uint Type { get; init; }
    public required uint Namespace { get; init; }
    public required uint Method { get; init; }
    public required uint Member { get; init; }
    public required uint Parameter { get; init; }
    public required uint String { get; init; }
    public required uint Number { get; init; }
    public required uint Comment { get; init; }
    public required uint Preprocessor { get; init; }
    public required uint ILOpcode { get; init; }
    public required uint ILLabel { get; init; }
    public required uint GutterText { get; init; }
    public required uint GutterSeparator { get; init; }
    public required uint FoldChip { get; init; }
    public required uint FoldChipHovered { get; init; }
    public required uint Occurrence { get; init; }
    public required uint FindMatch { get; init; }
    public required uint CurrentFind { get; init; }
    public required uint CurrentFindOutline { get; init; }
    public required uint Flash { get; init; }

    /// <summary>The gutter mark of an enabled bookmark.</summary>
    public required uint Bookmark { get; init; }

    public uint this[SpanKind kind] => kind switch
    {
        SpanKind.Keyword => Keyword,
        SpanKind.Type => Type,
        SpanKind.Namespace => Namespace,
        SpanKind.Method => Method,
        SpanKind.Field or SpanKind.Property or SpanKind.Event => Member,
        SpanKind.Parameter or SpanKind.Local => Parameter,
        SpanKind.String => String,
        SpanKind.Number => Number,
        SpanKind.Comment => Comment,
        SpanKind.Preprocessor => Preprocessor,
        SpanKind.ILOpcode => ILOpcode,
        SpanKind.ILLabel => ILLabel,
        _ => Plain,
    };

    public static CodeViewPalette Dark { get; } = new()
    {
        Plain = 0xFFD8DEE9,
        Keyword = 0xFF8FB8FF,
        Type = 0xFF88CEC3,
        Namespace = 0xFFD8DEE9,
        Method = 0xFFD7C58F,
        Member = 0xFFD8DEE9,
        Parameter = 0xFFB4C9E8,
        String = 0xFFDDB293,
        Number = 0xFFBACD9B,
        Comment = 0xFF879E84,
        Preprocessor = 0xFFA6ACB8,
        ILOpcode = 0xFF8FB8FF,
        ILLabel = 0xFFC3AADF,
        GutterText = 0xFF919BAA,
        GutterSeparator = 0xFF2D333D,
        FoldChip = 0xFF2A303A,
        FoldChipHovered = 0xFF363D49,
        Occurrence = 0xFF29354A,
        FindMatch = 0xFF493C25,
        CurrentFind = 0xFF5C4824,
        CurrentFindOutline = 0xFFE6BE73,
        Flash = 0xFF2E4470,
        Bookmark = 0xFF6CA6F0,
    };

    public static CodeViewPalette Light { get; } = new()
    {
        Plain = 0xFF293241,
        Keyword = 0xFF285CC4,
        Type = 0xFF146E68,
        Namespace = 0xFF293241,
        Method = 0xFF79551D,
        Member = 0xFF293241,
        Parameter = 0xFF365A88,
        String = 0xFF974322,
        Number = 0xFF526D27,
        Comment = 0xFF58704E,
        Preprocessor = 0xFF626A78,
        ILOpcode = 0xFF285CC4,
        ILLabel = 0xFF74449A,
        GutterText = 0xFF687487,
        GutterSeparator = 0xFFE3E7ED,
        FoldChip = 0xFFECEFF4,
        FoldChipHovered = 0xFFDFE4EB,
        Occurrence = 0xFFE5EDFA,
        FindMatch = 0xFFFFF0BF,
        CurrentFind = 0xFFFFE09A,
        CurrentFindOutline = 0xFF805500,
        Flash = 0xFFCFE0FF,
        Bookmark = 0xFF2F6FCF,
    };

    public static CodeViewPalette GitHubDark { get; } = new()
    {
        Plain = 0xFFE6EDF3,
        Keyword = 0xFFFF7B72,
        Type = 0xFFFFA657,
        Namespace = 0xFFE6EDF3,
        Method = 0xFFD2A8FF,
        Member = 0xFF79C0FF,
        Parameter = 0xFFE6EDF3,
        String = 0xFFA5D6FF,
        Number = 0xFF79C0FF,
        Comment = 0xFF8B949E,
        Preprocessor = 0xFFFF7B72,
        ILOpcode = 0xFFFF7B72,
        ILLabel = 0xFF7EE787,
        GutterText = 0xFF6E7681,
        GutterSeparator = 0xFF21262D,
        FoldChip = 0xFF21262D,
        FoldChipHovered = 0xFF30363D,
        Occurrence = 0xFF253040,
        FindMatch = 0xFF4A3E1E,
        CurrentFind = 0xFF6B5418,
        CurrentFindOutline = 0xFFD29922,
        Flash = 0xFF1F3A63,
        Bookmark = 0xFF58A6FF,
    };

    public static CodeViewPalette OneDarkPro { get; } = new()
    {
        Plain = 0xFFABB2BF,
        Keyword = 0xFFC678DD,
        Type = 0xFFE5C07B,
        Namespace = 0xFFABB2BF,
        Method = 0xFF61AFEF,
        Member = 0xFFE06C75,
        Parameter = 0xFFABB2BF,
        String = 0xFF98C379,
        Number = 0xFFD19A66,
        Comment = 0xFF7F848E,
        Preprocessor = 0xFFC678DD,
        ILOpcode = 0xFFC678DD,
        ILLabel = 0xFF56B6C2,
        GutterText = 0xFF636D83,
        GutterSeparator = 0xFF2C313A,
        FoldChip = 0xFF2C313A,
        FoldChipHovered = 0xFF3A3F4B,
        Occurrence = 0xFF3A404B,
        FindMatch = 0xFF33405A,
        CurrentFind = 0xFF42557B,
        CurrentFindOutline = 0xFF528BFF,
        Flash = 0xFF2F3E5C,
        Bookmark = 0xFF61AFEF,
    };

    public static CodeViewPalette Dracula { get; } = new()
    {
        Plain = 0xFFF8F8F2,
        Keyword = 0xFFFF79C6,
        Type = 0xFF8BE9FD,
        Namespace = 0xFFF8F8F2,
        Method = 0xFF50FA7B,
        Member = 0xFFF8F8F2,
        Parameter = 0xFFFFB86C,
        String = 0xFFF1FA8C,
        Number = 0xFFBD93F9,
        Comment = 0xFF6272A4,
        Preprocessor = 0xFFFF79C6,
        ILOpcode = 0xFFFF79C6,
        ILLabel = 0xFFBD93F9,
        GutterText = 0xFF6272A4,
        GutterSeparator = 0xFF343746,
        FoldChip = 0xFF343746,
        FoldChipHovered = 0xFF44475A,
        Occurrence = 0xFF3C3F52,
        FindMatch = 0xFF524335,
        CurrentFind = 0xFF6E523A,
        CurrentFindOutline = 0xFFFFB86C,
        Flash = 0xFF44475A,
        Bookmark = 0xFFBD93F9,
    };

    public static CodeViewPalette Monokai { get; } = new()
    {
        Plain = 0xFFF8F8F2,
        Keyword = 0xFFF92672,
        Type = 0xFF66D9EF,
        Namespace = 0xFFF8F8F2,
        Method = 0xFFA6E22E,
        Member = 0xFFF8F8F2,
        Parameter = 0xFFFD971F,
        String = 0xFFE6DB74,
        Number = 0xFFAE81FF,
        Comment = 0xFF88846F,
        Preprocessor = 0xFFF92672,
        ILOpcode = 0xFFF92672,
        ILLabel = 0xFFAE81FF,
        GutterText = 0xFF90908A,
        GutterSeparator = 0xFF3B3C35,
        FoldChip = 0xFF3B3C35,
        FoldChipHovered = 0xFF49483E,
        Occurrence = 0xFF3E3D32,
        FindMatch = 0xFF52502E,
        CurrentFind = 0xFF6E6A33,
        CurrentFindOutline = 0xFFE6DB74,
        Flash = 0xFF49483E,
        Bookmark = 0xFF66D9EF,
    };

    public static CodeViewPalette CatppuccinMocha { get; } = new()
    {
        Plain = 0xFFCDD6F4,
        Keyword = 0xFFCBA6F7,
        Type = 0xFFF9E2AF,
        Namespace = 0xFFCDD6F4,
        Method = 0xFF89B4FA,
        Member = 0xFFB4BEFE,
        Parameter = 0xFFEBA0AC,
        String = 0xFFA6E3A1,
        Number = 0xFFFAB387,
        Comment = 0xFF9399B2,
        Preprocessor = 0xFFF5C2E7,
        ILOpcode = 0xFFCBA6F7,
        ILLabel = 0xFF94E2D5,
        GutterText = 0xFF7F849C,
        GutterSeparator = 0xFF313244,
        FoldChip = 0xFF313244,
        FoldChipHovered = 0xFF45475A,
        Occurrence = 0xFF2E3047,
        FindMatch = 0xFF4B3B40,
        CurrentFind = 0xFF765A51,
        CurrentFindOutline = 0xFFFAB387,
        Flash = 0xFF2F3A5A,
        Bookmark = 0xFF89B4FA,
    };
}
