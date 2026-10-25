using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>The code view's editor colors: syntax by role, mapped to each <see cref="SpanKind"/>,
/// and the gutter, fold chip and highlight states. Carried per theme by <see cref="UiState"/>.</summary>
[ThemeColors]
public sealed partial class CodeViewPalette
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

    /// <summary>The background of the caret's line, when it is highlighted.</summary>
    public required uint CurrentLine { get; init; }

    /// <summary>The background of the bracket at the caret and the one matching it.</summary>
    public required uint MatchingBracket { get; init; }

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
}
