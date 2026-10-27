using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Diagrams;

namespace ZGF.ILSpy;

/// <summary>What a press on a diagram landed on: a card, and the row of it, or -1 for none; no card for
/// the space between them.</summary>
public readonly record struct DiagramHit(CardFace? Face, int Row)
{
    public static DiagramHit Nothing => new(null, -1);

    /// <summary>The row hit, or null when no row is.</summary>
    public CardRow? FindRow()
    {
        if (Face == null)
            return null;
        if (Row < 0)
            return null;
        return Face.Rows[Row];
    }
}

/// <summary>How a tab shows its diagram: where it looks and how near, the card selected, which parts of the
/// cards are open, whether members hidden from other assemblies show, the layout it moves to, the cards'
/// faces as last measured, the versions of the diagram last measured and laid out, what the press held now
/// landed on, the card its menu was opened on, and the tooltip shown. The first layout is fitted to the
/// view, a card focused is brought into view once a layout of its diagram as it is now places it, and the
/// card selected stays where it is on the screen while the cards move to a new layout.</summary>
public sealed class DiagramViewState
{
    public DiagramCamera Camera { get; } = new();

    public CardFolds Folds { get; } = new();

    public LayoutTransition Transition { get; } = new();

    public CardFaces Faces { get; } = new();

    /// <summary>What the diagram was last painted from.</summary>
    internal DiagramFrame Frame { get; } = new();

    /// <summary>The type whose card is selected, or null.</summary>
    public CodeReference? Selected;

    /// <summary>Whether members hidden from other assemblies show on the cards of types from assemblies
    /// other than the diagram's own.</summary>
    public bool ShowsHiddenMembers;

    /// <summary>Set until the first layout is fitted to the view.</summary>
    public bool IsFitPending = true;

    /// <summary>The type whose card is brought into view once it is laid out, or null.</summary>
    public CodeReference? PendingFocus;

    /// <summary>The version of the diagram whose cards were last measured, and a layout asked for.</summary>
    public int MeasuredVersion = -1;

    /// <summary>The version of the diagram the layout drawn was asked for, -1 before the first.</summary>
    public int LaidOutVersion = -1;

    /// <summary>What the press held now landed on.</summary>
    public DiagramHit PressedOn = DiagramHit.Nothing;

    /// <summary>Whether the press held now has moved far enough to pan.</summary>
    public bool PressMoved;

    /// <summary>The card the context menu was opened on.</summary>
    public TypeCard? MenuCard;

    public CodeTooltip? Tooltip;
    public int TooltipPart;

    /// <summary>Selects the card of <paramref name="type"/> and brings it into view once it is laid out.</summary>
    public void Focus(CodeReference type)
    {
        Selected = type;
        PendingFocus = type;
    }
}
