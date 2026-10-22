// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System;
using System.Linq;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// Renders a <see cref="FilterState"/> as a one-line predicate summary
	/// ("Visibility ∈ {Public, NestedPublic} ∧ Sealed ∧ ¬Abstract"). Lives next to
	/// <see cref="FilterState.Matches"/> so the visible string and the actual matching rule
	/// stay in sync; tests assert on the string.
	/// </summary>
	public static class FilterStatePresenter
	{
		public static string Describe(FilterState state)
		{
			if (state.IsEmpty)
				return "(any)";
			var clauses = new System.Collections.Generic.List<string>();
			foreach (var group in state.Schema.MutexGroups)
			{
				if (!state.MutexSelections.TryGetValue(group.Name, out var sel) || sel is null)
					continue;
				if (sel.IsEmpty)
				{
					clauses.Add($"{group.Name} ∈ ∅");
					continue;
				}
				var labels = group.Values
					.Where(v => sel.Contains(v.Value))
					.Select(v => StripHexSuffix(v.Label))
					.ToList();
				clauses.Add(labels.Count == 1
					? $"{group.Name} = {labels[0]}"
					: $"{group.Name} ∈ {{{string.Join(", ", labels)}}}");
			}
			var requiredFlags = state.Schema.IndependentFlags
				.Where(f => state.FlagStates.TryGetValue(f.Name, out var t) && t == TriState.Required)
				.Select(f => f.Name)
				.ToList();
			var excludedFlags = state.Schema.IndependentFlags
				.Where(f => state.FlagStates.TryGetValue(f.Name, out var t) && t == TriState.Excluded)
				.Select(f => f.Name)
				.ToList();
			if (requiredFlags.Count > 0)
			{
				var joiner = state.IndependentMode == MatchMode.All ? " ∧ " : " ∨ ";
				clauses.Add(requiredFlags.Count == 1
					? requiredFlags[0]
					: $"({string.Join(joiner, requiredFlags)})");
			}
			foreach (var name in excludedFlags)
				clauses.Add($"¬{name}");
			return string.Join(" ∧ ", clauses);
		}

		static string StripHexSuffix(string label)
		{
			// MutexValue labels carry a "(XXXX)" hex suffix from the inferer for disambiguation
			// in the dropdown; the predicate summary reads better without it ("Public" vs
			// "Public (0001)"). Strip the trailing parenthesised group, keep the name.
			int idx = label.LastIndexOf(' ');
			if (idx <= 0)
				return label;
			var rest = label.AsSpan(idx + 1);
			return rest.Length > 2 && rest[0] == '(' && rest[^1] == ')'
				? label[..idx]
				: label;
		}
	}
}
