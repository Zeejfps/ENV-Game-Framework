using ZGF.Geometry;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Diagrams;

namespace ZGF.ILSpy;

/// <summary>A card as one frame draws it: its face, the top-left of it on the diagram, whether it is
/// selected, and the row the pointer is on, or -1.</summary>
internal readonly record struct DrawnCard(CardFace Face, PointF Place, bool IsSelected, int HoveredRow);

/// <summary>A link as one frame draws it: how it relates its types, its route on the diagram, and its label
/// with the label's size at the diagram's own size, when it has one.</summary>
internal readonly record struct DrawnLink(TypeLinkKind Kind, LinkRoute Route, string? Label, float LabelWidth, float LabelHeight);

/// <summary>What one frame of a diagram is painted from, refilled each frame: the links, then the cards
/// over them, in the diagram's coordinates, drawn from <see cref="Corner"/> at <see cref="Zoom"/>.</summary>
internal sealed class DiagramFrame
{
    private const float LineWidth = 1.25f;
    private const float DashLength = 6f;
    private const float GapLength = 4f;
    private const float HollowArrowWidth = 12f;
    private const float OpenArrowWidth = 10f;
    private const float SelectedBorderWidth = 2f;
    private const float ChevronHalf = 3.5f;
    private const float OverlayInset = 0.75f;
    private const float MinTextSize = 3f;
    private const float MinIconSize = 4f;
    private const float TextRightPadding = 8f;

    /// <summary>How far below the middle of its row a link's underline runs, in font sizes.</summary>
    private const float UnderlineDrop = 0.5f;

    public readonly List<DrawnLink> Links = new();
    public readonly List<DrawnCard> Cards = new();
    public PointF Corner;
    public float Zoom = 1f;
    public UiTheme Theme = UiTheme.Dark;
    public NodeIconPalette Palette = BuiltInThemes.Dark.Icons;
    public AppIcons Icons = BuiltInIconThemes.Reference.Icons;

    public static void Paint(UiPainter p, DiagramFrame frame) => frame.Paint(p);

    private void Paint(UiPainter p)
    {
        foreach (var link in Links)
            PaintLink(p, link);
        foreach (var card in Cards)
            PaintCard(p, card);
    }

    private PointF ToScreen(UiPainter p, PointF point)
    {
        return new PointF(p.Rect.X + (point.X - Corner.X) * Zoom, p.Rect.Y + (point.Y - Corner.Y) * Zoom);
    }

    private float Stroke => MathF.Max(1f, LineWidth * Zoom);

    private void PaintLink(UiPainter p, DrawnLink link)
    {
        var color = Theme.TextMuted;
        var isDashed = link.Kind == TypeLinkKind.Implements;
        var dash = isDashed ? DashLength * Zoom : 0f;
        var gap = isDashed ? GapLength * Zoom : 0f;
        foreach (var piece in link.Route.Pieces)
            PaintPiece(p, piece, color, dash, gap);
        PaintArrowhead(p, link, color);
        PaintLabel(p, link);
    }

    private void PaintPiece(UiPainter p, RoutePiece piece, uint color, float dash, float gap)
    {
        var start = ToScreen(p, piece.Start);
        var control1 = ToScreen(p, piece.Control1);
        var control2 = ToScreen(p, piece.Control2);
        var end = ToScreen(p, piece.End);
        p.CubicBezier(start, control1, control2, end, Stroke, color, dash, gap);
    }

    /// <summary>A hollow triangle at the end of a link to a type derived from or implemented, an open
    /// arrowhead at the end of one to a type held.</summary>
    private void PaintArrowhead(UiPainter p, DrawnLink link, uint color)
    {
        var tip = ToScreen(p, link.Route.Tip);
        var tail = ToScreen(p, ArrowTailOf(link.Route));
        var length = MathF.Sqrt(tip.DistanceSqTo(tail));
        if (IsHeld(link.Kind))
        {
            p.OpenArrowhead(tip, tail, length, OpenArrowWidth * Zoom, Stroke, color);
            return;
        }
        p.HollowArrowhead(tip, tail, length, HollowArrowWidth * Zoom, Stroke, color);
    }

    /// <summary>Where the route's arrowhead starts: where its pieces end, or at its tip when it has none.</summary>
    private static PointF ArrowTailOf(LinkRoute route)
    {
        if (route.Pieces.Count == 0)
            return route.Tip;
        return route.Pieces[^1].End;
    }

    private static bool IsHeld(TypeLinkKind kind) => kind is TypeLinkKind.HoldsOne or TypeLinkKind.HoldsMany;

    private void PaintLabel(UiPainter p, DrawnLink link)
    {
        if (link.Label == null)
            return;
        var center = link.Route.LabelCenter;
        if (center == null)
            return;
        var size = Theme.FontSizeSmall * Zoom;
        if (size < MinTextSize)
            return;
        var middle = ToScreen(p, center.Value);
        var width = link.LabelWidth * Zoom;
        var height = link.LabelHeight * Zoom;
        var rect = new UiRect(middle.X - width / 2, middle.Y - height / 2, width, height);
        p.FillRect(rect, Theme.EditorBackground, Theme.RadiusSmall * Zoom);
        p.Text(rect, link.Label, Theme.TextMuted, size, align: Align.Center);
    }

    private void PaintCard(UiPainter p, DrawnCard card)
    {
        var face = card.Face;
        var topLeft = ToScreen(p, card.Place);
        var rect = new UiRect(topLeft.X, topLeft.Y, face.Width * Zoom, face.Height * Zoom);
        if (!rect.Intersects(p.Clip))
            return;
        var radius = Theme.CornerRadius * Zoom;
        p.FillRect(rect, Theme.SurfaceRaised, radius);
        p.FillRect(rect with { Height = face.HeaderHeight * Zoom }, Theme.Control, new Corners(radius, radius, 0f, 0f));
        for (var i = 0; i < face.Rows.Count; i++)
            PaintRow(p, rect, face.Rows[i], i == card.HoveredRow);
        PaintBorder(p, rect, card.IsSelected, radius);
    }

    private void PaintBorder(UiPainter p, UiRect rect, bool isSelected, float radius)
    {
        if (isSelected)
        {
            p.FillRect(rect, 0, radius, Theme.Accent, SelectedBorderWidth);
            return;
        }
        p.FillRect(rect, 0, radius, Theme.BorderStrong, Theme.BorderWidth);
    }

    private void PaintRow(UiPainter p, UiRect card, CardRow row, bool isHovered)
    {
        var rect = new UiRect(card.X, card.Y + row.Top * Zoom, card.Width, row.Height * Zoom);
        if (!rect.Intersects(p.Clip))
            return;
        if (isHovered)
            p.FillRect(rect, Theme.RowHover);
        var left = card.X + row.Indent * Zoom;
        var underlined = isHovered && row.IsLink;
        switch (row.Kind)
        {
            case CardRowKind.Name:
                PaintIconAndText(p, rect, left, row, Theme.Text, bold: true, underlined);
                break;
            case CardRowKind.Stereotype:
                PaintText(p, rect, left, row.Text, Theme.TextMuted, family: TextFonts.Italic);
                break;
            case CardRowKind.Fold:
                PaintFold(p, rect, left, row);
                break;
            default:
                PaintIconAndText(p, rect, left, row, Theme.Text, bold: false, underlined);
                break;
        }
    }

    private void PaintIconAndText(UiPainter p, UiRect rect, float left, CardRow row, uint color, bool bold, bool underlined)
    {
        PaintIcon(p, rect, left, row.Icon!.Value, row.Accessibility);
        var textLeft = left + (Theme.IconSize + DiagramFaces.IconGap) * Zoom;
        PaintText(p, rect, textLeft, row.Text, color, bold: bold);
        if (underlined)
            PaintUnderline(p, rect, textLeft, row.TextWidth * Zoom, color);
    }

    private void PaintUnderline(UiPainter p, UiRect rect, float left, float width, uint color)
    {
        var y = MathF.Round(rect.Y + rect.Height / 2 + Theme.FontSize * Zoom * UnderlineDrop);
        var thickness = MathF.Max(1f, Zoom);
        p.Line(new PointF(left, y), new PointF(left + width, y), thickness, color);
    }

    /// <summary>The icon of a node of <paramref name="kind"/> as the tree draws it, with the overlay of its
    /// <paramref name="accessibility"/>.</summary>
    private void PaintIcon(UiPainter p, UiRect row, float left, NodeKind kind, NodeAccessibility accessibility)
    {
        var size = MathF.Round(Theme.IconSize * Zoom);
        if (size < MinIconSize)
            return;
        var box = new UiRect(left, row.Y + (row.Height - size) / 2, size, size);
        p.Glyph(box, NodeIcons.Glyph(kind, Icons), Palette.For(kind), size);
        var overlay = NodeIcons.Overlay(accessibility, Icons);
        if (overlay == null)
            return;
        var overlaySize = MathF.Round(Theme.IconOverlaySize * Zoom);
        var corner = new UiRect(box.Right - overlaySize * OverlayInset, box.Bottom - overlaySize * OverlayInset, overlaySize, overlaySize);
        p.Glyph(corner, overlay.Value, Palette.Restricted, overlaySize);
    }

    private void PaintFold(UiPainter p, UiRect rect, float left, CardRow row)
    {
        var chevronWidth = DiagramFaces.ChevronWidth * Zoom;
        var center = new PointF(left + chevronWidth / 2, rect.Y + rect.Height / 2);
        p.DrawDisclosure(center, ChevronHalf * Zoom, row.IsOpen, Theme.TextMuted, Theme.IconStrokeWidth * Zoom);
        PaintText(p, rect, left + chevronWidth, row.Text, Theme.TextMuted);
    }

    private void PaintText(UiPainter p, UiRect rect, float left, string text, uint color, bool bold = false, string? family = null)
    {
        var size = Theme.FontSize * Zoom;
        if (size < MinTextSize)
            return;
        var width = rect.Right - TextRightPadding * Zoom - left;
        p.Text(new UiRect(left, rect.Y, width, rect.Height), text, color, size, bold, family: family);
    }
}
