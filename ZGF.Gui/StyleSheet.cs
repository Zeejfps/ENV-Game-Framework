namespace ZGF.Gui;

public sealed class StyleSheet
{
    private readonly List<StyleRule> _rules = new();

    public IReadOnlyList<StyleRule> Rules => _rules;

    /// <summary>
    /// Primitive rule registration. Within a specificity tier, rules added earlier
    /// win against rules added later (registration order is the stable tie-breaker
    /// applied during cascade).
    /// </summary>
    public void AddRule(Selector selector, Style style)
    {
        _rules.Add(new StyleRule(selector, style));
    }

    public void AddStyleForClass(string classId, Style style)
    {
        AddRule(new Selector(classId: classId), style);
    }

    public void AddStyleForId(string id, Style style)
    {
        AddRule(new Selector(id: id), style);
    }

    /// <summary>
    /// Yields rules whose selector matches <paramref name="view"/>, ordered by
    /// ascending specificity (low → high). Within a tier, original sheet registration
    /// order is preserved. The cascade applies them in this order, so higher-specificity
    /// rules naturally overwrite lower ones.
    /// </summary>
    public IEnumerable<StyleRule> RulesMatching(View view)
    {
        // Group by tier in a stable way: list out all matching rules with their index,
        // then stable-sort by SpecificityTier ascending. Within a tier, original index
        // order is preserved.
        var matched = new List<(int Index, StyleRule Rule)>();
        for (var i = 0; i < _rules.Count; i++)
        {
            var rule = _rules[i];
            if (rule.Selector.Matches(view))
                matched.Add((i, rule));
        }

        matched.Sort((a, b) =>
        {
            var tierCmp = a.Rule.Selector.SpecificityTier.CompareTo(b.Rule.Selector.SpecificityTier);
            return tierCmp != 0 ? tierCmp : a.Index.CompareTo(b.Index);
        });

        foreach (var (_, rule) in matched)
            yield return rule;
    }
}
