// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ZGF.ILSpy.Core.Metadata
{
	public enum TriState { DontCare, Required, Excluded }

	public enum MatchMode { All, Any }

	/// <summary>What a <see cref="FilterState"/> constrains, to keep between runs: the values each
	/// narrowed group allows and the flags not left don't-care, by name.</summary>
	public sealed record SavedFilterState(Dictionary<string, uint[]>? Groups, Dictionary<string, TriState>? Flags, MatchMode Mode);

	/// <summary>
	/// Mutable per-column user state for the schema-driven flag filter. The popup UI
	/// pokes at this; <see cref="Matches"/> applies it to a row's value. <c>null</c> in
	/// <see cref="MutexSelections"/> means the user picked "any" for that group — every
	/// row passes that axis.
	/// </summary>
	public sealed class FilterState
	{
		readonly Dictionary<string, ImmutableHashSet<uint>?> mutexSelections = new();
		readonly Dictionary<string, TriState> flagStates = new();
		MatchMode independentMode = MatchMode.All;
		int compiledRevision = -1;
		uint requiredMask;
		uint excludedMask;

		public FlagsSchema Schema { get; }

		public IReadOnlyDictionary<string, ImmutableHashSet<uint>?> MutexSelections => mutexSelections;

		public IReadOnlyDictionary<string, TriState> FlagStates => flagStates;

		/// <summary>Changes whenever the state does, for views to notice.</summary>
		public int Revision { get; private set; }

		public MatchMode IndependentMode {
			get => independentMode;
			set {
				if (independentMode == value)
					return;
				independentMode = value;
				Revision++;
			}
		}

		public FilterState(FlagsSchema schema)
		{
			Schema = schema ?? throw new ArgumentNullException(nameof(schema));
			foreach (var group in schema.MutexGroups)
				mutexSelections[group.Name] = null;
			foreach (var flag in schema.IndependentFlags)
				flagStates[flag.Name] = TriState.DontCare;
		}

		public void SetMutexSelection(string groupName, ImmutableHashSet<uint>? values)
		{
			mutexSelections[groupName] = values;
			Revision++;
		}

		public void SetFlagState(string flagName, TriState state)
		{
			flagStates[flagName] = state;
			Revision++;
		}

		/// <summary>Resets every group to "any" and every flag to don't-care.</summary>
		public void Clear()
		{
			foreach (var key in mutexSelections.Keys.ToArray())
				mutexSelections[key] = null;
			foreach (var key in flagStates.Keys.ToArray())
				flagStates[key] = TriState.DontCare;
			independentMode = MatchMode.All;
			Revision++;
		}

		/// <summary>True when no constraints are active — the predicate degenerates to "match all".</summary>
		public bool IsEmpty =>
			mutexSelections.Values.All(static s => s is null) && flagStates.Values.All(static s => s == TriState.DontCare);

		/// <summary>Whether a row whose flags are <paramref name="value"/> passes: it holds an allowed
		/// value of every narrowed group, none of the excluded flags, and all or any of the required ones.</summary>
		public bool Matches(uint value)
		{
			if (compiledRevision != Revision)
				Compile();
			if ((value & excludedMask) != 0)
				return false;
			foreach (var group in Schema.MutexGroups)
			{
				if (mutexSelections.GetValueOrDefault(group.Name) is { } allowed && !allowed.Contains(value & group.Mask))
					return false;
			}
			return requiredMask == 0
				|| (independentMode == MatchMode.All ? (value & requiredMask) == requiredMask : (value & requiredMask) != 0);
		}

		void Compile()
		{
			requiredMask = excludedMask = 0;
			foreach (var flag in Schema.IndependentFlags)
			{
				var state = flagStates.GetValueOrDefault(flag.Name);
				if (state == TriState.Required)
					requiredMask |= flag.Bit;
				else if (state == TriState.Excluded)
					excludedMask |= flag.Bit;
			}
			compiledRevision = Revision;
		}

		/// <summary>The constraints to keep, or null when there are none.</summary>
		public SavedFilterState? Save() =>
			IsEmpty
				? null
				: new SavedFilterState(
					mutexSelections.Where(static s => s.Value is not null).ToDictionary(static s => s.Key, static s => s.Value!.Order().ToArray()),
					flagStates.Where(static f => f.Value != TriState.DontCare).ToDictionary(static f => f.Key, static f => f.Value),
					independentMode);

		/// <summary>Applies what <see cref="Save"/> kept, skipping groups and flags the schema no longer has.</summary>
		public void Restore(SavedFilterState saved)
		{
			foreach (var (group, values) in saved.Groups ?? new())
			{
				if (mutexSelections.ContainsKey(group))
					mutexSelections[group] = values.ToImmutableHashSet();
			}
			foreach (var (flag, state) in saved.Flags ?? new())
			{
				if (flagStates.ContainsKey(flag))
					flagStates[flag] = state;
			}
			independentMode = saved.Mode;
			Revision++;
		}
	}
}
