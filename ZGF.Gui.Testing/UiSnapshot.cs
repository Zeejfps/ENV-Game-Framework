using System.Text;
using System.Text.Json;
using ZGF.Geometry;

namespace ZGF.Gui.Testing;

/// <summary>One node of a <see cref="UiSnapshot"/>: a laid-out view reduced to the facts an LLM
/// needs to reason about the screen — what it is, what it says, where it is, and what state it's in.
/// <see cref="Text"/> is a <c>TextView</c>'s own text; <see cref="Label"/> is an accessible name
/// (explicit, or aggregated from descendant text for an interactive node).</summary>
public sealed record UiNode(
    string Type,
    string? Id,
    AccessibilityRole Role,
    string? Label,
    string? Text,
    RectF Bounds,
    bool Visible,
    float Opacity,
    bool Clips,
    IReadOnlyList<string> States,
    IReadOnlyList<UiNode> Children)
{
    /// <summary>The accessible name shown for this node: its own text if it is text, else its label.</summary>
    public string? DisplayLabel => Text ?? Label;

    public IEnumerable<UiNode> SelfAndDescendants()
    {
        yield return this;
        foreach (var child in Children)
            foreach (var d in child.SelfAndDescendants())
                yield return d;
    }
}

/// <summary>A textual, diffable rendering of a laid-out view tree — the LLM's "screenshot in words".
/// Built by <see cref="SnapshotBuilder"/> / <c>GuiTestHarness.Snapshot</c>; render it with
/// <see cref="ToText"/> (for reading and failure messages) or <see cref="ToJson"/> (for scripting).</summary>
public sealed record UiSnapshot(UiNode Root)
{
    /// <summary>Indented, integer-rounded, line-oriented tree — hidden subtrees collapse to a single
    /// <c>(Type #id hidden)</c> marker. Stable across runs so two snapshots diff cleanly.</summary>
    public string ToText()
    {
        var sb = new StringBuilder();
        AppendNode(sb, Root, 0);
        return sb.ToString();
    }

    public string ToJson() =>
        JsonSerializer.Serialize(Project(Root), new JsonSerializerOptions { WriteIndented = true });

    public override string ToString() => ToText();

    /// <summary>Reports what changed from this snapshot to <paramref name="other"/> — appeared/removed
    /// nodes and per-node label/visibility/state changes — so "what did my click do?" is one call.
    /// Nodes are matched by <see cref="UiNode.Id"/> when present, else by type + sibling order. Bounds
    /// are intentionally ignored (animation churn); the focus is semantic change.</summary>
    public string DiffTo(UiSnapshot other)
    {
        var lines = new List<string>();
        DiffNode(Root, other.Root, lines);
        return lines.Count == 0 ? "(no changes)" : string.Join("\n", lines);
    }

    private static void DiffNode(UiNode a, UiNode b, List<string> lines)
    {
        if (a.Visible != b.Visible)
            lines.Add($"~ {Desc(b)} {(b.Visible ? "shown" : "hidden")}");
        if (a.DisplayLabel != b.DisplayLabel)
            lines.Add($"~ {Desc(b)} label {Quote(a.DisplayLabel)} -> {Quote(b.DisplayLabel)}");

        foreach (var state in b.States)
            if (!a.States.Contains(state)) lines.Add($"~ {Desc(b)} +{state}");
        foreach (var state in a.States)
            if (!b.States.Contains(state)) lines.Add($"~ {Desc(b)} -{state}");

        var aChildren = KeyChildren(a);
        var bChildren = KeyChildren(b);
        var bKeys = new HashSet<string>(bChildren.Select(x => x.Key));
        var aKeys = aChildren.ToDictionary(x => x.Key, x => x.Node);

        foreach (var (key, child) in aChildren)
            if (!bKeys.Contains(key)) lines.Add($"- {Desc(child)} (removed)");
        foreach (var (key, child) in bChildren)
        {
            if (aKeys.TryGetValue(key, out var aChild)) DiffNode(aChild, child, lines);
            else lines.Add($"+ {Desc(child)} (added)");
        }
    }

    private static List<(string Key, UiNode Node)> KeyChildren(UiNode n)
    {
        var result = new List<(string, UiNode)>(n.Children.Count);
        var counts = new Dictionary<string, int>();
        foreach (var c in n.Children)
        {
            string key;
            if (c.Id != null)
            {
                key = "#" + c.Id;
            }
            else
            {
                var i = counts.TryGetValue(c.Type, out var x) ? x : 0;
                counts[c.Type] = i + 1;
                key = c.Type + "#" + i;
            }
            result.Add((key, c));
        }
        return result;
    }

    private static string Desc(UiNode n)
    {
        var name = n.Id != null ? "#" + n.Id : n.Type;
        var label = n.DisplayLabel;
        return string.IsNullOrEmpty(label) ? name : $"{name} \"{label}\"";
    }

    private static string Quote(string? s) => s == null ? "(none)" : $"\"{s}\"";

    private static void AppendNode(StringBuilder sb, UiNode n, int depth)
    {
        sb.Append(' ', depth * 2);

        if (!n.Visible)
        {
            sb.Append('(').Append(n.Type);
            if (n.Id != null) sb.Append(" #").Append(n.Id);
            sb.Append(" hidden)").Append('\n');
            return; // collapsed: a hidden subtree contributes nothing more
        }

        sb.Append(n.Type);
        if (n.Id != null) sb.Append(" #").Append(n.Id);
        if (!string.IsNullOrEmpty(n.Role.Name)) sb.Append(" role=").Append(n.Role.Name);

        var label = n.DisplayLabel;
        if (!string.IsNullOrEmpty(label)) sb.Append(" \"").Append(Escape(label)).Append('"');

        var b = n.Bounds;
        sb.Append(" [").Append(R(b.Left)).Append(',').Append(R(b.Bottom))
          .Append(' ').Append(R(b.Width)).Append('x').Append(R(b.Height)).Append(']');

        if (n.Opacity < 1f) sb.Append(" opacity=").Append(n.Opacity.ToString("0.##"));
        if (n.Clips) sb.Append(" clip");
        foreach (var state in n.States) sb.Append(' ').Append(state);
        sb.Append('\n');

        foreach (var child in n.Children)
            AppendNode(sb, child, depth + 1);
    }

    private static object Project(UiNode n) => new
    {
        type = n.Type,
        id = n.Id,
        role = string.IsNullOrEmpty(n.Role.Name) ? null : n.Role.Name,
        label = n.Label,
        text = n.Text,
        bounds = new { x = R(n.Bounds.Left), y = R(n.Bounds.Bottom), w = R(n.Bounds.Width), h = R(n.Bounds.Height) },
        visible = n.Visible,
        opacity = n.Opacity,
        clips = n.Clips,
        states = n.States,
        children = n.Children.Select(Project).ToArray(),
    };

    private static int R(float v) => (int)MathF.Round(v);

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
}
