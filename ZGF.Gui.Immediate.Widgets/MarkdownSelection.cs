using System.Text;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>A caret in shown markdown: a character of one leaf's joined text. Leaves are numbered in
/// reading order, so positions order as the text reads.</summary>
internal readonly record struct MarkdownPos(int Leaf, int Char) : IComparable<MarkdownPos>
{
    public int CompareTo(MarkdownPos other)
    {
        var byLeaf = Leaf.CompareTo(other.Leaf);
        return byLeaf != 0 ? byLeaf : Char.CompareTo(other.Char);
    }
}

/// <summary>A selection across the leaves of one shown markdown text: a double press selects a word
/// of a leaf, a triple press the whole leaf.</summary>
internal sealed class MarkdownSelection : TextSelection<IReadOnlyList<MarkdownLeaf>, MarkdownPos>
{
    /// <summary>The selected characters of <paramref name="leaf"/>, empty when none are; the one rule
    /// the highlight and the copy share, so they never disagree. The leaves at the ends are cut at
    /// the caret and anchor; those between are taken whole.</summary>
    public (int Start, int End) SpanIn(int leaf, int length)
    {
        if (!HasSelection || leaf < Start.Leaf || leaf > End.Leaf)
            return default;
        var from = leaf == Start.Leaf ? Math.Min(Start.Char, length) : 0;
        var to = leaf == End.Leaf ? Math.Min(End.Char, length) : length;
        return from < to ? (from, to) : default;
    }

    /// <summary>The selected text: what each leaf shows, without the markup that styled it, a table
    /// row's cells joined by tabs and other leaves by line breaks.</summary>
    public string Of(IReadOnlyList<MarkdownLeaf> leaves)
    {
        var text = new StringBuilder();
        var last = Math.Min(End.Leaf, leaves.Count - 1);
        for (var leaf = Start.Leaf; leaf <= last; leaf++)
        {
            if (leaf > Start.Leaf)
                text.Append(leaves[leaf].Before);
            var (from, to) = SpanIn(leaf, leaves[leaf].Text.Length);
            text.Append(leaves[leaf].Text, from, to - from);
        }
        return text.ToString();
    }

    /// <summary>Selects every leaf, start to end.</summary>
    public void SelectAll(IReadOnlyList<MarkdownLeaf> leaves)
    {
        if (leaves.Count == 0)
            return;
        Select(new MarkdownPos(0, 0), new MarkdownPos(leaves.Count - 1, leaves[^1].Text.Length));
    }

    public void Clear() => Select(default, default);

    protected override (MarkdownPos Start, MarkdownPos End) WordAt(IReadOnlyList<MarkdownLeaf> leaves, MarkdownPos at)
    {
        TextBoundaries.WordAt(leaves[at.Leaf].Text, at.Char, out var start, out var end, splitPunctuation: true);
        return (at with { Char = start }, at with { Char = end });
    }

    protected override (MarkdownPos Start, MarkdownPos End) BlockAt(IReadOnlyList<MarkdownLeaf> leaves, MarkdownPos at) =>
        (at with { Char = 0 }, at with { Char = leaves[at.Leaf].Text.Length });
}
