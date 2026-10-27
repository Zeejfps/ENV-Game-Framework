using ZGF.Geometry;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Diagrams;

namespace ZGF.ILSpy;

/// <summary>The layout a diagram is drawn in, and while a newer one comes in, the move from the one
/// before: cards glide from their old places to their new ones, and links bend from their old routes to
/// their new ones, over a fixed time. Cards and links the old layout had not appear in place.</summary>
public sealed class LayoutTransition
{
    private const float Seconds = 0.3f;

    private TypeLayout? _from;
    private TypeLayout? _to;
    private Dictionary<TypeLink, LinkRoute> _fromRoutes = new();
    private float _elapsed = Seconds;

    /// <summary>The layout drawn, or moved to; null before the first.</summary>
    public TypeLayout? Layout => _to;

    public bool IsMoving => _elapsed < Seconds;

    /// <summary>Starts moving to <paramref name="layout"/> when it is another than <see cref="Layout"/>;
    /// the first layout is drawn at once. True when it was another.</summary>
    public bool Follow(TypeLayout? layout)
    {
        if (ReferenceEquals(layout, _to))
            return false;
        _from = _to;
        _to = layout;
        _fromRoutes = RoutesByLink(_from);
        _elapsed = StartOf(_from);
        return true;
    }

    private static Dictionary<TypeLink, LinkRoute> RoutesByLink(TypeLayout? layout)
    {
        var routes = new Dictionary<TypeLink, LinkRoute>();
        if (layout == null)
            return routes;
        foreach (var route in layout.Routes)
            routes.TryAdd(route.Link, route);
        return routes;
    }

    /// <summary>How far into the move a move from <paramref name="from"/> starts: at its end when there is
    /// nothing to move from.</summary>
    private static float StartOf(TypeLayout? from)
    {
        if (from == null)
            return Seconds;
        return 0f;
    }

    public void Advance(float seconds)
    {
        _elapsed = MathF.Min(Seconds, _elapsed + seconds);
    }

    private float Progress => Ease.OutCubic(_elapsed / Seconds);

    /// <summary>Where the top-left of the card of <paramref name="type"/> is drawn now; null when the
    /// layout does not place it.</summary>
    public PointF? PlaceOf(CodeReference type)
    {
        if (_to == null)
            return null;
        if (!_to.Places.TryGetValue(type, out var to))
            return null;
        var from = FindOldPlace(type);
        if (from == null)
            return to;
        return RoutePiece.Lerp(from.Value, to, Progress);
    }

    private PointF? FindOldPlace(CodeReference type)
    {
        if (!IsMoving)
            return null;
        if (_from == null)
            return null;
        if (!_from.Places.TryGetValue(type, out var from))
            return null;
        return from;
    }

    /// <summary>How <paramref name="route"/>, one of the layout's, is drawn now: bent from the route its
    /// link had before while moving, else as it is.</summary>
    public LinkRoute Shape(LinkRoute route)
    {
        if (!IsMoving)
            return route;
        if (!_fromRoutes.TryGetValue(route.Link, out var from))
            return route;
        if (!CanBlend(from, route))
            return route;
        return Blend(from, route, Progress);
    }

    private static bool CanBlend(LinkRoute from, LinkRoute to)
    {
        if (from.Pieces.Count == 0)
            return false;
        return to.Pieces.Count > 0;
    }

    private static LinkRoute Blend(LinkRoute from, LinkRoute to, float t)
    {
        var fromPieces = Subdivide(from.Pieces, to.Pieces.Count);
        var toPieces = Subdivide(to.Pieces, from.Pieces.Count);
        var pieces = new List<RoutePiece>(toPieces.Count);
        for (var i = 0; i < toPieces.Count; i++)
            pieces.Add(fromPieces[i].Blend(toPieces[i], t));
        var tip = RoutePiece.Lerp(from.Tip, to.Tip, t);
        return to with { Pieces = pieces, Tip = tip, LabelCenter = BlendLabel(from, to, t) };
    }

    /// <summary><paramref name="pieces"/>, halved one after another, until there are at least
    /// <paramref name="count"/> of them.</summary>
    private static List<RoutePiece> Subdivide(IReadOnlyList<RoutePiece> pieces, int count)
    {
        var split = pieces.ToList();
        var next = 0;
        while (split.Count < count)
        {
            var (first, second) = split[next].Split();
            split[next] = first;
            split.Insert(next + 1, second);
            next = (next + 2) % split.Count;
        }
        return split;
    }

    private static PointF? BlendLabel(LinkRoute from, LinkRoute to, float t)
    {
        if (from.LabelCenter == null)
            return to.LabelCenter;
        if (to.LabelCenter == null)
            return null;
        return RoutePiece.Lerp(from.LabelCenter.Value, to.LabelCenter.Value, t);
    }
}
