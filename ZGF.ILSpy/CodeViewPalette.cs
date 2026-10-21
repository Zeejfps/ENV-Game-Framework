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
    };
}
