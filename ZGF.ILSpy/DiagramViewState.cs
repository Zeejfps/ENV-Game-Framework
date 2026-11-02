using ZGF.Geometry;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Diagrams;

namespace ZGF.ILSpy;

/// <summary>What a press on a diagram landed on: nothing, in the space between the cards; a card, off its
/// rows; or a row of a card.</summary>
public abstract record DiagramHit
{
    private DiagramHit()
    {
    }

    public sealed record Nothing : DiagramHit;

    public sealed record Card(CardFace Face) : DiagramHit;

    /// <summary>The row <see cref="Index"/> of <see cref="Face"/>.</summary>
    public sealed record Row(CardFace Face, int Index) : DiagramHit
    {
        public CardRow Value => Face.Rows[Index];
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
    public CodeReference.Entity? Selected;

    private int _filterChanges;

    /// <summary>Whether members hidden from other assemblies show on the cards of types from assemblies
    /// other than the diagram's own.</summary>
    public bool ShowsHiddenMembers { get; private set; }

    /// <summary>Counts the changes to what the settings keep of the view: where it looks and how near,
    /// the folds, and the filter.</summary>
    public int Changes => Camera.Changes + Folds.Version + _filterChanges;

    /// <summary>Shows members hidden from other assemblies on every card when <paramref name="shows"/>.</summary>
    public void ShowHiddenMembers(bool shows)
    {
        if (shows == ShowsHiddenMembers)
            return;
        ShowsHiddenMembers = shows;
        _filterChanges++;
    }

    /// <summary>Set until the first layout is fitted to the view.</summary>
    public bool IsFitPending = true;

    /// <summary>The type whose card is brought into view once it is laid out, or null.</summary>
    public CodeReference.Entity? PendingFocus;

    /// <summary>The version of the diagram whose cards were last measured, and a layout asked for.</summary>
    public int MeasuredVersion = -1;

    /// <summary>The version of the diagram the layout drawn was asked for, -1 before the first.</summary>
    public int LaidOutVersion = -1;

    /// <summary>What the press held now landed on.</summary>
    public DiagramHit PressedOn = new DiagramHit.Nothing();

    /// <summary>Whether the press held now has moved far enough to pan.</summary>
    public bool PressMoved;

    /// <summary>The card the context menu was opened on.</summary>
    public TypeCard? MenuCard;

    internal KeptTooltip Tooltip { get; } = new();

    /// <summary>Selects the card of <paramref name="type"/> and brings it into view once it is laid out.</summary>
    public void Focus(CodeReference.Entity type)
    {
        Selected = type;
        PendingFocus = type;
    }

    /// <summary>What the settings keep of the view.</summary>
    public DiagramViewRecord Save()
    {
        var corner = Camera.Corner;
        return new DiagramViewRecord(corner.X, corner.Y, Camera.Zoom, Folds.Save(), ShowsHiddenMembers);
    }

    /// <summary>Returns the view to how <paramref name="saved"/> says it was, which the first layout then
    /// keeps rather than fitting itself to the view.</summary>
    public void Restore(DiagramViewRecord saved)
    {
        Camera.Restore(new PointF(saved.X, saved.Y), saved.Zoom);
        RestoreFolds(saved.Folds);
        ShowsHiddenMembers = saved.ShowsHiddenMembers;
        IsFitPending = false;
    }

    private void RestoreFolds(List<CardFold>? folds)
    {
        if (folds == null)
            return;
        Folds.Restore(folds);
    }
}
