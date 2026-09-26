using ZGF.Geometry;
using ZGF.Gui.Desktop.Inspection;

namespace ZGF.Gui.Testing;

/// <summary>
/// Checks a laid-out tree against the two things pixel-grid layout promises: every view sits on its
/// window's device-pixel grid, and layout comes to rest instead of moving something every frame.
/// </summary>
public static class PixelGridAudit
{
    private const float DeviceTolerance = 1f / 256f;

    /// <summary>Views that lay out unrounded, or have an edge off their grid, described one per line.</summary>
    public static IReadOnlyList<string> OffGrid(View root)
    {
        var problems = new List<string>();
        foreach (var view in root.SelfAndDescendants())
        {
            if (!view.IsVisible) continue;
            if (view.PixelGrid is not { } grid)
            {
                problems.Add($"{Describe(view)} lays out on no pixel grid");
                continue;
            }

            var p = view.Position;
            if (!OnGrid(p.Left, grid) || !OnGrid(p.Bottom, grid) || !OnGrid(p.Right, grid) || !OnGrid(p.Top, grid))
                problems.Add($"{Describe(view)} at {Format(p)} is off the {grid.Scale}x grid");
        }
        return problems;
    }

    /// <summary>
    /// Lays the tree out the way a host does each frame, then once more, and describes every view
    /// whose position or visibility changed between the two — a layout that is still moving after a
    /// frame, such as a scrollbar that flips on and off.
    /// </summary>
    public static IReadOnlyList<string> Unsettled(View root)
    {
        root.LayoutUntilSettled();
        var before = root.SelfAndDescendants().ToDictionary(v => v, v => (v.Position, v.IsVisible));
        root.LayoutUntilSettled();

        var problems = new List<string>();
        if (root.NeedsLayout)
            problems.Add($"{Describe(root)} still needs layout after the most passes a frame allows");
        foreach (var view in root.SelfAndDescendants())
        {
            if (!before.TryGetValue(view, out var was))
            {
                problems.Add($"{Describe(view)} appeared on a second layout pass");
                continue;
            }
            if (was != (view.Position, view.IsVisible))
                problems.Add($"{Describe(view)} moved from {Format(was.Position)} to {Format(view.Position)}");
        }
        return problems;
    }

    /// <summary>Throws with every problem <see cref="OffGrid"/> and <see cref="Unsettled"/> find.</summary>
    public static void AssertSettledOnPixelGrid(View root)
    {
        var problems = Unsettled(root).Concat(OffGrid(root)).ToList();
        if (problems.Count == 0) return;
        const int shown = 20;
        var more = problems.Count > shown ? $"{Environment.NewLine}… and {problems.Count - shown} more" : "";
        throw new InvalidOperationException(
            $"Layout is not settled on the pixel grid:{Environment.NewLine}" +
            string.Join(Environment.NewLine, problems.Take(shown)) + more);
    }

    private static bool OnGrid(float logical, PixelGrid grid)
    {
        var device = logical * grid.Scale;
        return MathF.Abs(device - MathF.Round(device)) <= DeviceTolerance;
    }

    private static string Describe(View view) =>
        view.Id is { } id ? $"{view.GetType().Name} '{id}'" : view.GetType().Name;

    private static string Format(RectF r) => $"({r.Left}, {r.Bottom}, {r.Width}x{r.Height})";
}
