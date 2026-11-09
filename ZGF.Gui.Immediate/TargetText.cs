using System.Text;
using ZGF.Geometry;

namespace ZGF.Gui.Immediate;

/// <summary>Says why a <see cref="TargetResult"/> that is no success failed, as a harness reports it.</summary>
internal static class TargetText
{
    public static string Problem(TargetResult result)
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
        return text.ToString();
    }

    private static void AppendNodes(StringBuilder text, IReadOnlyList<SemanticNode> nodes)
    {
        foreach (var node in nodes)
            text.Append("\n  ").Append(node);
    }

    private static string Point(PointF at) => $"({MathF.Round(at.X)},{MathF.Round(at.Y)})";
}
