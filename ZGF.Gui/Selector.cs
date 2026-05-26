namespace ZGF.Gui;

/// <summary>
/// Identifies the views a style rule applies to. A selector matches a view when
/// <see cref="ClassId"/> (if non-null) is in the view's style classes, every entry
/// in <see cref="Modifiers"/> is in the view's modifiers, and <see cref="Id"/> (if
/// non-null) equals the view's id.
/// </summary>
public sealed record Selector
{
    public string? ClassId { get; }
    public IReadOnlyList<string> Modifiers { get; }
    public string? Id { get; }

    public Selector(string? classId = null, IReadOnlyList<string>? modifiers = null, string? id = null)
    {
        ClassId = classId;
        Modifiers = modifiers ?? Array.Empty<string>();
        Id = id;
    }

    /// <summary>
    /// Selector specificity bucket. Id presence dominates; modifier count breaks ties
    /// among equal id-presence selectors. Within a tier, sheet registration order is the
    /// tie-breaker (handled by the cascade, not the selector).
    /// </summary>
    public int SpecificityTier => (Id != null ? 1000 : 0) + Modifiers.Count;

    public bool Matches(View view)
    {
        if (ClassId != null && !view.StyleClasses.Contains(ClassId))
            return false;

        if (Id != null && view.Id != Id)
            return false;

        for (var i = 0; i < Modifiers.Count; i++)
        {
            if (!view.StyleModifiers.Contains(Modifiers[i]))
                return false;
        }

        return true;
    }
}

public sealed record StyleRule(Selector Selector, Style Style);
