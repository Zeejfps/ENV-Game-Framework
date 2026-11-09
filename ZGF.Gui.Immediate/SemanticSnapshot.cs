using System.Buffers;
using System.Text;
using System.Text.Json;
using ZGF.Geometry;

namespace ZGF.Gui.Immediate;

/// <summary>
/// A completed frame as a test sees it: a tree of <see cref="SemanticNode"/>s built after layout,
/// clipping and focus traversal, with what the pointer would reach where. Coordinates are the main
/// window's UI points, from its top-left, y down; popups and windows of their own are placed in the
/// same space and name their <see cref="SemanticSurface"/>.
/// </summary>
public sealed class SemanticSnapshot
{
    private const int NearestCount = 5;

    private readonly HitRegion[] _hits;
    private readonly IReadOnlyList<SemanticNode?> _modals;
    private readonly IReadOnlyDictionary<int, SemanticNode> _byHitId;
    private readonly bool _pointerLocked;

    internal SemanticSnapshot(long frame, SemanticNode root, IReadOnlyList<SemanticSurface> surfaces, HitRegion[] hits,
        IReadOnlyList<SemanticNode?> modals, IReadOnlyDictionary<int, SemanticNode> byHitId, bool pointerLocked)
    {
        Frame = frame;
        Root = root;
        Surfaces = surfaces;
        _hits = hits;
        _modals = modals;
        _byHitId = byHitId;
        _pointerLocked = pointerLocked;
    }

    /// <summary>The frame it was built at the end of.</summary>
    public long Frame { get; }

    /// <summary>The main window's content.</summary>
    public SemanticNode Root { get; }

    /// <summary>The main window, index 0, then every popup and window shown in a window of its own.</summary>
    public IReadOnlyList<SemanticSurface> Surfaces { get; }

    public IEnumerable<SemanticNode> Nodes => Root.SelfAndDescendants();

    /// <summary>The one node <paramref name="selector"/> names: <see cref="TargetResult.Found"/>, or
    /// <see cref="TargetResult.NotFound"/> or <see cref="TargetResult.Ambiguous"/>.</summary>
    public TargetResult Resolve(Selector selector)
    {
        var matches = Matches(selector);
        if (matches.Count == 1)
            return new TargetResult.Found(matches[0]);
        if (matches.Count > 1)
            return new TargetResult.Ambiguous(selector, matches);
        return new TargetResult.NotFound(selector, Nearest(selector));
    }

    /// <summary>The one node <paramref name="selector"/> names and where a press reaches it, through the
    /// same topmost-hit and modal rules input follows: <see cref="TargetResult.Hittable"/>, or why not.</summary>
    public TargetResult Target(Selector selector)
    {
        var resolved = Resolve(selector);
        if (resolved is not TargetResult.Found found)
            return resolved;
        return Reach(found.Node);
    }

    /// <summary>Whether a press at <paramref name="at"/> reaches <paramref name="node"/>, and if not, why.</summary>
    public TargetResult ReachAt(SemanticNode node, PointF at)
    {
        var blocked = Blocking(node);
        if (blocked != null)
            return blocked;
        if (Reaches(node, at))
            return new TargetResult.Hittable(node, at);
        return new TargetResult.Occluded(node, NodeHitAt(at), at);
    }

    /// <summary>A point of <paramref name="node"/> a press reaches: the middle of the largest piece of it that
    /// no hit region above its own covers.</summary>
    private TargetResult Reach(SemanticNode node)
    {
        var blocked = Blocking(node);
        if (blocked != null)
            return blocked;
        var area = node.VisibleBounds;
        var center = new PointF(area.X + area.Width / 2, area.Y + area.Height / 2);
        var own = OwnRegion(node);
        if (own == null)
            return new TargetResult.Occluded(node, NodeHitAt(center), center);

        var exposed = area.Intersect(own.Value.Rect);
        var pieces = new List<UiRect> { exposed };
        var rest = new List<UiRect>();
        foreach (var hit in _hits)
        {
            if (hit.Id == node.HitId || !Above(hit, own.Value) || !hit.Rect.Intersects(exposed)) continue;
            Subtract(pieces, hit.Rect, rest);
            (pieces, rest) = (rest, pieces);
            if (pieces.Count == 0) break;
        }
        foreach (var piece in pieces.OrderByDescending(r => r.Width * r.Height))
        {
            var at = new PointF(piece.X + piece.Width / 2, piece.Y + piece.Height / 2);
            if (Reaches(node, at))
                return new TargetResult.Hittable(node, at);
        }
        return new TargetResult.Occluded(node, NodeHitAt(center), center);
    }

    /// <summary>The hit region the pointer reaches <paramref name="node"/> by, over what of it is shown: the topmost of its hit id's.</summary>
    private HitRegion? OwnRegion(SemanticNode node)
    {
        HitRegion? best = null;
        foreach (var hit in _hits)
        {
            if (hit.Id != node.HitId || !hit.Rect.Intersects(node.VisibleBounds)) continue;
            if (best == null || Above(hit, best.Value))
                best = hit;
        }
        return best;
    }

    /// <summary>Whether <paramref name="hit"/> wins over <paramref name="other"/> where both are, as input decides.</summary>
    private static bool Above(HitRegion hit, HitRegion other) => hit.Z > other.Z || (hit.Z == other.Z && hit.Order > other.Order);

    /// <summary>Fills <paramref name="rest"/> with what of <paramref name="pieces"/> lies outside <paramref name="cut"/>, every bit of it.</summary>
    private static void Subtract(List<UiRect> pieces, UiRect cut, List<UiRect> rest)
    {
        rest.Clear();
        foreach (var piece in pieces)
        {
            if (!piece.Intersects(cut))
            {
                rest.Add(piece);
                continue;
            }
            var top = MathF.Max(piece.Y, cut.Y);
            var bottom = MathF.Min(piece.Bottom, cut.Bottom);
            AddIfAny(rest, new UiRect(piece.X, piece.Y, piece.Width, top - piece.Y));
            AddIfAny(rest, new UiRect(piece.X, bottom, piece.Width, piece.Bottom - bottom));
            AddIfAny(rest, new UiRect(piece.X, top, cut.X - piece.X, bottom - top));
            AddIfAny(rest, new UiRect(cut.Right, top, piece.Right - cut.Right, bottom - top));
        }
    }

    private static void AddIfAny(List<UiRect> pieces, UiRect piece)
    {
        if (piece.Width > 0f && piece.Height > 0f)
            pieces.Add(piece);
    }

    private TargetResult? Blocking(SemanticNode node)
    {
        if (node.Visibility is Visibility.ScrolledOut or Visibility.Hidden)
            return new TargetResult.Clipped(node);
        if (_pointerLocked)
            return new TargetResult.Unreachable(node, "the pointer is locked, so presses reach nothing");
        if (node.PassThrough)
            return new TargetResult.Unreachable(node, "it is inside something that lets the pointer through, such as a tooltip");
        if (node.HitId == 0)
            return new TargetResult.Unreachable(node, "neither it nor anything around it takes the pointer");
        for (var k = _modals.Count - 1; k > node.Layer; k--)
        {
            var modal = _modals[k];
            if (modal != null)
                return new TargetResult.Blocked(node, modal);
        }
        return null;
    }

    private bool Reaches(SemanticNode node, PointF at)
    {
        if (node.HitId == 0 || !node.VisibleBounds.Contains(at)) return false;
        return Ui.TopmostAt(_hits, at, out _) == node.HitId;
    }

    private SemanticNode? NodeHitAt(PointF at)
    {
        var id = Ui.TopmostAt(_hits, at, out _);
        if (id == 0) return null;
        return _byHitId.GetValueOrDefault(id);
    }

    private List<SemanticNode> Matches(Selector selector)
    {
        switch (selector)
        {
            case Selector.ByPath byPath:
                return Nodes.Where(n => byPath.Path.Equals(n.Path)).ToList();
            case Selector.ByRole byRole:
                return Nodes.Where(n => n.Role == byRole.Role && n.Label == byRole.Label).ToList();
            case Selector.ByText byText:
                var labelled = Nodes.Where(n => n.Label == byText.Text).ToList();
                return labelled.Where(n => !labelled.Any(other => other != n && other.IsWithin(n))).ToList();
            default:
                throw new InvalidOperationException($"Unknown selector {selector}.");
        }
    }

    private IReadOnlyList<SemanticNode> Nearest(Selector selector)
    {
        var (wanted, key) = selector switch
        {
            Selector.ByPath byPath => (byPath.Path.ToString(), (Func<SemanticNode, string?>)(n => n.Path?.ToString())),
            Selector.ByRole byRole => (byRole.Label, n => n.Label),
            Selector.ByText byText => (byText.Text, n => n.Label),
            _ => throw new InvalidOperationException($"Unknown selector {selector}."),
        };
        return Nodes
            .Select(n => (Node: n, Key: key(n)))
            .SelectMany(c => c.Key == null ? [] : new[] { (c.Node, Distance: Distance(wanted, c.Key)) })
            .OrderBy(c => c.Distance)
            .Take(NearestCount)
            .Select(c => c.Node)
            .ToList();
    }

    private static int Distance(string a, string b)
    {
        a = a.ToLowerInvariant();
        b = b.ToLowerInvariant();
        if (b.Contains(a) || a.Contains(b))
            return Math.Abs(a.Length - b.Length) / 4;
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
            previous[j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var substitution = previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1);
                current[j] = Math.Min(substitution, Math.Min(previous[j] + 1, current[j - 1] + 1));
            }
            (previous, current) = (current, previous);
        }
        return previous[b.Length];
    }

    /// <summary>One node per line, indented by depth: role, #path, "label", [x,y wxh], value and states.
    /// A node scrolled out or hidden is shown in parentheses without what is inside it. The header says
    /// how coordinates read and lists the surfaces.</summary>
    public string ToText() => SemanticText.Write(this);

    /// <summary><see cref="ToText"/>'s content as JSON.</summary>
    public string ToJson() => SemanticText.WriteJson(this);

    public override string ToString() => ToText();
}

internal static class SemanticText
{
    public const string Coordinates = "[x,y wxh] in main-window UI points, origin top-left, y down";

    public static string Write(SemanticSnapshot snapshot)
    {
        var text = new StringBuilder();
        text.Append("# frame ").Append(snapshot.Frame).Append(": ").Append(Coordinates).Append('\n');
        foreach (var surface in snapshot.Surfaces)
        {
            text.Append("# surface ").Append(SurfaceName(surface)).Append(' ');
            AppendRect(text, surface.Rect);
            text.Append(" @").Append(surface.DpiScale.ToString("0.##")).Append("x\n");
        }
        AppendNode(text, snapshot.Root, 0);
        return text.ToString();
    }

    public static string Line(SemanticNode node)
    {
        var text = new StringBuilder();
        AppendLine(text, node);
        return text.ToString();
    }

    private static void AppendNode(StringBuilder text, SemanticNode node, int depth)
    {
        text.Append(' ', depth * 2);
        AppendLine(text, node);
        text.Append('\n');
        if (Collapsed(node)) return;
        foreach (var child in node.Children)
            AppendNode(text, child, depth + 1);
    }

    private static bool Collapsed(SemanticNode node) => node.Visibility is Visibility.ScrolledOut or Visibility.Hidden;

    private static void AppendLine(StringBuilder text, SemanticNode node)
    {
        var collapsed = Collapsed(node);
        if (collapsed)
            text.Append('(');
        text.Append(RoleName(node.Role));
        if (node.Path != null)
            text.Append(" #").Append(node.Path);
        if (!string.IsNullOrEmpty(node.Label))
            text.Append(" \"").Append(Escape(node.Label)).Append('"');
        text.Append(' ');
        AppendRect(text, node.Bounds);
        if (node.Value != null)
            text.Append(" value=\"").Append(Escape(node.Value)).Append('"');
        foreach (var state in StateNames(node.States))
            text.Append(' ').Append(state);
        if (node.Visibility != Visibility.Visible)
            text.Append(' ').Append(VisibilityName(node.Visibility));
        if (node.Surface.Kind != SurfaceKind.Main)
            text.Append(" on ").Append(SurfaceName(node.Surface));
        if (collapsed)
            text.Append(')');
    }

    private static void AppendRect(StringBuilder text, UiRect rect) =>
        text.Append('[').Append(R(rect.X)).Append(',').Append(R(rect.Y)).Append(' ').Append(R(rect.Width)).Append('x').Append(R(rect.Height)).Append(']');

    public static string WriteJson(SemanticSnapshot snapshot)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("frame", snapshot.Frame);
            writer.WriteString("coordinates", Coordinates);
            writer.WriteStartArray("surfaces");
            foreach (var surface in snapshot.Surfaces)
            {
                writer.WriteStartObject();
                writer.WriteString("kind", surface.Kind.ToString().ToLowerInvariant());
                writer.WriteNumber("index", surface.Index);
                WriteRect(writer, "rect", surface.Rect);
                writer.WriteNumber("dpiScale", surface.DpiScale);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WritePropertyName("root");
            WriteNode(writer, snapshot.Root);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteNode(Utf8JsonWriter w, SemanticNode node)
    {
        w.WriteStartObject();
        w.WriteString("role", RoleName(node.Role));
        WriteStringOrNull(w, "path", node.Path?.ToString());
        WriteStringOrNull(w, "label", node.Label);
        WriteStringOrNull(w, "value", node.Value);
        WriteRect(w, "bounds", node.Bounds);
        w.WriteString("visibility", VisibilityName(node.Visibility));
        w.WriteNumber("surface", node.Surface.Index);
        w.WriteStartArray("states");
        foreach (var state in StateNames(node.States))
            w.WriteStringValue(state);
        w.WriteEndArray();
        w.WriteStartArray("children");
        foreach (var child in node.Children)
            WriteNode(w, child);
        w.WriteEndArray();
        w.WriteEndObject();
    }

    private static void WriteRect(Utf8JsonWriter w, string name, UiRect rect)
    {
        w.WriteStartObject(name);
        w.WriteNumber("x", R(rect.X));
        w.WriteNumber("y", R(rect.Y));
        w.WriteNumber("w", R(rect.Width));
        w.WriteNumber("h", R(rect.Height));
        w.WriteEndObject();
    }

    private static void WriteStringOrNull(Utf8JsonWriter w, string name, string? value)
    {
        if (value == null) w.WriteNull(name);
        else w.WriteString(name, value);
    }

    public static string RoleName(SemanticRole role) => role.ToString().ToLowerInvariant();

    private static string VisibilityName(Visibility visibility) => visibility switch
    {
        Visibility.Visible => "visible",
        Visibility.Clipped => "clipped",
        Visibility.ScrolledOut => "scrolled-out",
        Visibility.Hidden => "hidden",
        _ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null),
    };

    private static string SurfaceName(SemanticSurface surface) => surface.Kind switch
    {
        SurfaceKind.Main => "main",
        SurfaceKind.Popup => $"popup {surface.Index}",
        SurfaceKind.Window => $"window {surface.Index}",
        _ => throw new ArgumentOutOfRangeException(nameof(surface), surface.Kind, null),
    };

    private static IEnumerable<string> StateNames(SemanticStates states)
    {
        foreach (var state in Enum.GetValues<SemanticStates>())
        {
            if (state != SemanticStates.None && (states & state) != 0)
                yield return state.ToString().ToLowerInvariant();
        }
    }

    private static int R(float v) => (int)MathF.Round(v);

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
}
