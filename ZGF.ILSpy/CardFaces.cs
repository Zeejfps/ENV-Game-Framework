using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Diagrams;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>What a row of a card shows.</summary>
public enum CardRowKind
{
    /// <summary>The type's icon and name.</summary>
    Name,

    /// <summary>Whether the type is abstract, static or an interface.</summary>
    Stereotype,

    /// <summary>The title of a part that folds, with how many members it holds.</summary>
    Fold,

    /// <summary>A member's icon and label.</summary>
    Member,
}

/// <summary>A row of a card at the diagram's own size: what it shows, how tall it is and how far in its
/// content starts, its text and icon, what it stands for: the type or member it opens, or the part it
/// folds, open or not; where it starts below the card's top, and how wide its text is.</summary>
public readonly record struct CardRow(
    CardRowKind Kind,
    float Height,
    float Indent,
    string Text,
    NodeIcon? Icon = null,
    NodeAccessibility Accessibility = NodeAccessibility.None,
    CodeReference? Target = null,
    CardFold? Fold = null,
    bool IsOpen = false,
    float Top = 0f,
    float TextWidth = 0f)
{
    public float Bottom => Top + Height;

    /// <summary>Whether a click on the row opens the code of what it shows.</summary>
    public bool IsLink => Target != null;
}

/// <summary>How a card is drawn at the diagram's own size: its rows from the top, how wide and tall it is,
/// and how far down its header reaches.</summary>
public sealed record CardFace(TypeCard Card, IReadOnlyList<CardRow> Rows, float Width, float Height, float HeaderHeight)
{
    /// <summary>The index of the row <paramref name="y"/> below the card's top falls in, or -1.</summary>
    public int RowAt(float y)
    {
        for (var i = 0; i < Rows.Count; i++)
        {
            if (y >= Rows[i].Top && y < Rows[i].Bottom)
                return i;
        }
        return -1;
    }
}

/// <summary>What a diagram's card faces are measured for: the cards, which parts of them are open, whether
/// members hidden from other assemblies show, the font and the language.</summary>
public readonly record struct FaceKey(int DiagramVersion, int FoldsVersion, bool ShowsHiddenMembers, float FontSize, string? FontFamily, Strings Strings);

/// <summary>The faces of a diagram's cards as last measured, and what they were measured for, to measure
/// them again only when that changes.</summary>
public sealed class CardFaces
{
    private readonly Dictionary<CodeReference, CardFace> _faces = new();
    private FaceKey? _measuredFor;

    public bool IsMeasuredFor(FaceKey key) => _measuredFor == key;

    /// <summary>Keeps <paramref name="faces"/>, measured for <paramref name="key"/>, in place of those before.</summary>
    public void Keep(FaceKey key, IReadOnlyList<CardFace> faces)
    {
        _faces.Clear();
        foreach (var face in faces)
            _faces[face.Card.Type] = face;
        _measuredFor = key;
    }

    /// <summary>The face of the card of <paramref name="type"/>, or null when none was measured.</summary>
    public CardFace? Find(CodeReference type) => _faces.GetValueOrDefault(type);
}
