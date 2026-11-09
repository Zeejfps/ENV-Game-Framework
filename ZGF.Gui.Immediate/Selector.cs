using ZGF.Geometry;

namespace ZGF.Gui.Immediate;

/// <summary>How a test names the node it means: by its <see cref="TestPath"/>, by its role and label,
/// or by the text it shows.</summary>
public abstract record Selector
{
    private Selector()
    {
    }

    /// <summary>The node recorded under exactly <see cref="Path"/>.</summary>
    public sealed record ByPath(TestPath Path) : Selector
    {
        public override string ToString() => SemanticText.PathText(Path);
    }

    /// <summary>The node of <see cref="Role"/> labelled exactly <see cref="Label"/>.</summary>
    public sealed record ByRole(SemanticRole Role, string Label) : Selector
    {
        public override string ToString() => $"{SemanticText.RoleName(Role)} \"{Label}\"";
    }

    /// <summary>The innermost node labelled exactly <see cref="Text"/>: a text, or a widget whose label it is.</summary>
    public sealed record ByText(string Text) : Selector
    {
        public override string ToString() => $"text \"{Text}\"";
    }

    /// <summary>The node at <paramref name="path"/>, written as <see cref="TestPath.ToString"/> writes it.</summary>
    public static Selector AtPath(string path) => new ByPath(TestPath.Parse(path));

    public static Selector OfRole(SemanticRole role, string label) => new ByRole(role, label);

    public static Selector WithText(string text) => new ByText(text);
}

/// <summary>What looking for a <see cref="Selector"/> in a <see cref="SemanticSnapshot"/> found, and
/// whether the pointer could reach it.</summary>
public abstract record TargetResult
{
    private TargetResult()
    {
    }

    /// <summary>Exactly one node matched.</summary>
    public sealed record Found(SemanticNode Node) : TargetResult;

    /// <summary>Exactly one node matched, and a press at <see cref="At"/> reaches it.</summary>
    public sealed record Hittable(SemanticNode Node, PointF At) : TargetResult;

    /// <summary>No node matched; <see cref="Nearest"/> are the closest there were.</summary>
    public sealed record NotFound(Selector Selector, IReadOnlyList<SemanticNode> Nearest) : TargetResult;

    /// <summary>More than one node matched.</summary>
    public sealed record Ambiguous(Selector Selector, IReadOnlyList<SemanticNode> Matches) : TargetResult;

    /// <summary>Something else takes the pointer everywhere over the node: <see cref="By"/>, or
    /// nothing a snapshot shows when null, at <see cref="At"/>.</summary>
    public sealed record Occluded(SemanticNode Node, SemanticNode? By, PointF At) : TargetResult;

    /// <summary>A <see cref="Ui.Modal"/> opened after the node was recorded keeps the pointer from it.</summary>
    public sealed record Blocked(SemanticNode Node, SemanticNode Modal) : TargetResult;

    /// <summary>No press reaches the node wherever it is, for <see cref="Reason"/>: the pointer is locked, the
    /// node lets the pointer through, or neither it nor anything around it takes the pointer.</summary>
    public sealed record Unreachable(SemanticNode Node, string Reason) : TargetResult;

    /// <summary>None of the node is shown: see its <see cref="SemanticNode.Visibility"/>.</summary>
    public sealed record Clipped(SemanticNode Node) : TargetResult;
}
