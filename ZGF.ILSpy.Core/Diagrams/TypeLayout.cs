using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
using ZGF.Geometry;
using MsaglPoint = Microsoft.Msagl.Core.Geometry.Point;
using MsaglRectangle = Microsoft.Msagl.Core.Geometry.Rectangle;

namespace ZGF.ILSpy.Core.Diagrams;

/// <summary>A card of a diagram as wide and tall as it is drawn.</summary>
public readonly record struct MeasuredCard(CodeReference Type, float Width, float Height);

/// <summary>A link of a diagram with the size of its label, zero when it has none.</summary>
public readonly record struct MeasuredLink(TypeLink Link, float LabelWidth, float LabelHeight);

/// <summary>What a diagram is laid out from: the size of each card and each link's label.</summary>
public sealed record DiagramMeasures(IReadOnlyList<MeasuredCard> Cards, IReadOnlyList<MeasuredLink> Links);

/// <summary>A piece of a link's route: a cubic Bézier curve, or a straight line drawn as one.</summary>
public readonly record struct RoutePiece(PointF Start, PointF Control1, PointF Control2, PointF End)
{
    private const float OneThird = 1f / 3f;
    private const float TwoThirds = 2f / 3f;

    /// <summary>The straight line from <paramref name="start"/> to <paramref name="end"/>, its controls on it.</summary>
    public static RoutePiece Line(PointF start, PointF end) => new(start, Lerp(start, end, OneThird), Lerp(start, end, TwoThirds), end);

    private const float Half = 0.5f;

    /// <summary>The point <paramref name="t"/> of the way from <paramref name="from"/> to <paramref name="to"/>.</summary>
    public static PointF Lerp(PointF from, PointF to, float t) => new(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t);

    /// <summary>The two halves of the curve, split at its middle.</summary>
    public (RoutePiece First, RoutePiece Second) Split()
    {
        var startSide = Lerp(Start, Control1, Half);
        var middleSide = Lerp(Control1, Control2, Half);
        var endSide = Lerp(Control2, End, Half);
        var firstControl = Lerp(startSide, middleSide, Half);
        var secondControl = Lerp(middleSide, endSide, Half);
        var middle = Lerp(firstControl, secondControl, Half);
        return (new RoutePiece(Start, startSide, firstControl, middle), new RoutePiece(middle, secondControl, endSide, End));
    }

    /// <summary>The curve <paramref name="t"/> of the way from this one to <paramref name="other"/>,
    /// each point moved alike.</summary>
    public RoutePiece Blend(RoutePiece other, float t)
    {
        return new RoutePiece(Lerp(Start, other.Start, t), Lerp(Control1, other.Control1, t), Lerp(Control2, other.Control2, t), Lerp(End, other.End, t));
    }
}

/// <summary>Where a link runs: from its source card, or the far point of its diamond, to the base of its
/// arrowhead in <see cref="Pieces"/>, the arrowhead's tip on its target card, the center of its label when
/// it has one, and the diamond's tip on its source card when it holds its target.</summary>
public sealed record LinkRoute(TypeLink Link, IReadOnlyList<RoutePiece> Pieces, PointF Tip, PointF? LabelCenter, PointF? SourceTip = null);

/// <summary>Where a diagram's cards go and its links run, in layers with the types derived from and held
/// above those deriving from and holding them, as MSAGL's layered layout places them. Coordinates run
/// right and down from the top-left of the whole layout, which is <see cref="Width"/> by
/// <see cref="Height"/>.</summary>
public sealed class TypeLayout
{
    /// <summary>How long the arrowhead at the end of a route is.</summary>
    public const float ArrowLength = 12f;

    /// <summary>How long the diamond at the start of a route to a type held is.</summary>
    public const float DiamondLength = 16f;

    private const double LayerSeparation = 56;
    private const double NodeSeparation = 32;
    private const int SamplesPerCurve = 8;

    private TypeLayout(Dictionary<CodeReference, PointF> places, List<LinkRoute> routes, float width, float height)
    {
        Places = places;
        Routes = routes;
        Width = width;
        Height = height;
    }

    /// <summary>The top-left corner of each card.</summary>
    public IReadOnlyDictionary<CodeReference, PointF> Places { get; }

    public IReadOnlyList<LinkRoute> Routes { get; }

    public float Width { get; }

    public float Height { get; }

    /// <summary>Lays out the cards of <paramref name="measures"/> and routes the links between them; a
    /// link to a card not measured is left out.</summary>
    public static TypeLayout Arrange(DiagramMeasures measures, CancellationToken cancellationToken = default)
    {
        if (measures.Cards.Count == 0)
            return new TypeLayout(new Dictionary<CodeReference, PointF>(), [], 0f, 0f);
        var graph = new GeometryGraph();
        var nodes = AddNodes(graph, measures.Cards);
        var edges = AddEdges(graph, nodes, measures.Links);
        Calculate(graph, cancellationToken);
        var box = graph.BoundingBox;
        var origin = new MsaglPoint(box.Left, box.Bottom);
        return new TypeLayout(PlacesOf(nodes, origin), RoutesOf(edges, origin), (float)box.Width, (float)box.Height);
    }

    private static Dictionary<CodeReference, Node> AddNodes(GeometryGraph graph, IReadOnlyList<MeasuredCard> cards)
    {
        var nodes = new Dictionary<CodeReference, Node>();
        foreach (var card in cards)
        {
            var boundary = CurveFactory.CreateRectangle(card.Width, card.Height, new MsaglPoint());
            var node = new Node(boundary, card.Type);
            graph.Nodes.Add(node);
            nodes[card.Type] = node;
        }
        return nodes;
    }

    private static List<Edge> AddEdges(GeometryGraph graph, Dictionary<CodeReference, Node> nodes, IReadOnlyList<MeasuredLink> links)
    {
        var edges = new List<Edge>();
        foreach (var link in links)
        {
            var edge = CreateEdge(nodes, link);
            if (edge == null)
                continue;
            graph.Edges.Add(edge);
            edges.Add(edge);
        }
        return edges;
    }

    private static Edge? CreateEdge(Dictionary<CodeReference, Node> nodes, MeasuredLink link)
    {
        if (!nodes.TryGetValue(link.Link.From, out var source))
            return null;
        if (!nodes.TryGetValue(link.Link.To, out var target))
            return null;
        var edge = new Edge(source, target) { UserData = link.Link };
        edge.EdgeGeometry.TargetArrowhead = new Arrowhead { Length = ArrowLength };
        if (IsHeld(link.Link.Kind))
            edge.EdgeGeometry.SourceArrowhead = new Arrowhead { Length = DiamondLength };
        if (HasLabel(link))
            edge.Label = new Label(link.LabelWidth, link.LabelHeight, edge);
        return edge;
    }

    private static bool IsHeld(TypeLinkKind kind) => kind is TypeLinkKind.HoldsOne or TypeLinkKind.HoldsMany;

    private static bool HasLabel(MeasuredLink link)
    {
        if (link.LabelWidth <= 0f)
            return false;
        return link.LabelHeight > 0f;
    }

    /// <summary>Lays out <paramref name="graph"/>, stopping with <see cref="OperationCanceledException"/>
    /// once <paramref name="cancellationToken"/> is cancelled.</summary>
    private static void Calculate(GeometryGraph graph, CancellationToken cancellationToken)
    {
        var settings = new SugiyamaLayoutSettings { LayerSeparation = LayerSeparation, NodeSeparation = NodeSeparation };
        var cancel = new CancelToken();
        using var registration = cancellationToken.Register(Cancel, cancel);
        LayoutHelpers.CalculateLayout(graph, settings, cancel);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static void Cancel(object? cancel)
    {
        var token = (CancelToken)cancel!;
        token.Canceled = true;
    }

    private static Dictionary<CodeReference, PointF> PlacesOf(Dictionary<CodeReference, Node> nodes, MsaglPoint origin)
    {
        var places = new Dictionary<CodeReference, PointF>();
        foreach (var (type, node) in nodes)
            places[type] = TopLeftOf(node.BoundingBox, origin);
        return places;
    }

    /// <summary>The corner of <paramref name="box"/> nearest <paramref name="origin"/>, which the layout
    /// draws at its top-left: MSAGL's Y runs up, with the types derived from lowest, and the layout is
    /// drawn with it running down.</summary>
    private static PointF TopLeftOf(MsaglRectangle box, MsaglPoint origin) => Place(new MsaglPoint(box.Left, box.Bottom), origin);

    private static PointF Place(MsaglPoint point, MsaglPoint origin) => new((float)(point.X - origin.X), (float)(point.Y - origin.Y));

    private static List<LinkRoute> RoutesOf(List<Edge> edges, MsaglPoint origin)
    {
        var routes = new List<LinkRoute>();
        foreach (var edge in edges)
        {
            var route = RouteOf(edge, origin);
            if (route != null)
                routes.Add(route);
        }
        return routes;
    }

    private static LinkRoute? RouteOf(Edge edge, MsaglPoint origin)
    {
        var curve = edge.Curve;
        if (curve == null)
            return null;
        var pieces = new List<RoutePiece>();
        AddPieces(pieces, curve, origin);
        var tip = Place(edge.EdgeGeometry.TargetArrowhead.TipPosition, origin);
        return new LinkRoute((TypeLink)edge.UserData, pieces, tip, LabelCenterOf(edge, origin), SourceTipOf(edge, origin));
    }

    private static PointF? SourceTipOf(Edge edge, MsaglPoint origin)
    {
        var diamond = edge.EdgeGeometry.SourceArrowhead;
        if (diamond == null)
            return null;
        return Place(diamond.TipPosition, origin);
    }

    private static PointF? LabelCenterOf(Edge edge, MsaglPoint origin)
    {
        if (edge.Label == null)
            return null;
        return Place(edge.Label.Center, origin);
    }

    /// <summary>Adds <paramref name="curve"/> as pieces: its lines and Bézier segments as they are, and
    /// any other segment as lines through points along it.</summary>
    private static void AddPieces(List<RoutePiece> pieces, ICurve curve, MsaglPoint origin)
    {
        switch (curve)
        {
            case Curve compound:
                foreach (var segment in compound.Segments)
                    AddPieces(pieces, segment, origin);
                break;
            case LineSegment line:
                pieces.Add(RoutePiece.Line(Place(line.Start, origin), Place(line.End, origin)));
                break;
            case CubicBezierSegment bezier:
                pieces.Add(BezierPiece(bezier, origin));
                break;
            default:
                AddSampled(pieces, curve, origin);
                break;
        }
    }

    private static RoutePiece BezierPiece(CubicBezierSegment bezier, MsaglPoint origin)
    {
        return new RoutePiece(Place(bezier.B(0), origin), Place(bezier.B(1), origin), Place(bezier.B(2), origin), Place(bezier.B(3), origin));
    }

    private static void AddSampled(List<RoutePiece> pieces, ICurve curve, MsaglPoint origin)
    {
        var step = (curve.ParEnd - curve.ParStart) / SamplesPerCurve;
        var previous = Place(curve.Start, origin);
        for (var i = 1; i <= SamplesPerCurve; i++)
        {
            var next = Place(curve[curve.ParStart + step * i], origin);
            pieces.Add(RoutePiece.Line(previous, next));
            previous = next;
        }
    }
}
