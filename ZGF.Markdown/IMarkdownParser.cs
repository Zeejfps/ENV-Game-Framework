namespace ZGF.Markdown;

/// <summary>
/// The parser seam: markdown text in, <see cref="MarkdownDocument"/> out. Implementations must
/// never throw — unsupported or malformed syntax degrades to literal text, and any prefix of a
/// valid document parses cleanly (streaming re-parses on every delta). Swapping backends means
/// writing one adapter that produces the same AST.
/// </summary>
public interface IMarkdownParser
{
    /// <summary>Parses <paramref name="text"/> into a block tree. Never throws.</summary>
    MarkdownDocument Parse(string text);
}
