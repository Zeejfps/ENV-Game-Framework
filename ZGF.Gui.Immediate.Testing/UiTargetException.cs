using System.Text;

namespace ZGF.Gui.Immediate.Testing;

/// <summary>A <see cref="Selector"/> named no node, more than one, or one a press would not reach;
/// <see cref="Result"/> says which. The message holds the snapshot it was looked for in.</summary>
public sealed class UiTargetException : Exception
{
    private UiTargetException(TargetResult result, string message, Exception? inner = null) : base(message, inner)
    {
        Result = result;
    }

    public TargetResult Result { get; }

    internal static UiTargetException For(TargetResult result, SemanticSnapshot snapshot) => new(result, Describe(result, snapshot));

    /// <summary>This failure with <paramref name="note"/> put first in its message.</summary>
    internal UiTargetException WithNote(string note) => new(Result, note + "\n" + Message, this);

    internal static string Describe(TargetResult result, SemanticSnapshot snapshot)
    {
        var text = new StringBuilder();
        switch (result)
        {
            case TargetResult.NotFound notFound:
                text.Append("Nothing matches ").Append(notFound.Selector)
                    .Append(". Rows a virtual list or tree does not show are not recorded: scroll them into view first.");
                if (notFound.Nearest.Count > 0)
                    AppendNodes(text.Append("\nNearest:"), notFound.Nearest);
                break;
            case TargetResult.Ambiguous ambiguous:
                AppendNodes(text.Append(ambiguous.Matches.Count).Append(" nodes match ").Append(ambiguous.Selector).Append(':'), ambiguous.Matches);
                break;
            case TargetResult.Occluded occluded:
                text.Append(occluded.Node).Append(" is covered at ").Append(Point(occluded.At)).Append(" by ")
                    .Append(occluded.By?.ToString() ?? "an element no snapshot node stands for").Append(", and everywhere else over it; not clicked.");
                break;
            case TargetResult.Blocked blocked:
                text.Append(blocked.Node).Append(" is blocked by the modal ").Append(blocked.Modal).Append("; not clicked.");
                break;
            case TargetResult.Unreachable unreachable:
                text.Append(unreachable.Node).Append(" cannot be pressed: ").Append(unreachable.Reason).Append("; not clicked.");
                break;
            case TargetResult.Clipped clipped:
                text.Append(clipped.Node).Append(" is not shown; not clicked.");
                break;
            case TargetResult.Found or TargetResult.Hittable:
                throw new ArgumentException($"{result} is no failure.", nameof(result));
            default:
                throw new ArgumentOutOfRangeException(nameof(result), result, null);
        }
        return text.Append("\n\n").Append(snapshot.ToText()).ToString();
    }

    private static void AppendNodes(StringBuilder text, IReadOnlyList<SemanticNode> nodes)
    {
        foreach (var node in nodes)
            text.Append("\n  ").Append(node);
    }

    private static string Point(ZGF.Geometry.PointF at) => $"({MathF.Round(at.X)},{MathF.Round(at.Y)})";
}
