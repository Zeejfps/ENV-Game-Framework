// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ZGF.ILSpy.Core.Metadata
{
	public enum TriState { DontCare, Required, Excluded }

	public enum MatchMode { All, Any }

	/// <summary>
	/// Mutable per-column user state for the schema-driven flag filter. The popup UI
	/// pokes at this; <see cref="CompiledFilter.Compile"/> snapshots it into an
	/// evaluation-friendly form. <c>null</c> in <see cref="MutexSelections"/> means the
	/// user picked "any" for that group — every row passes that axis.
	/// </summary>
	public sealed class FilterState
	{
		public FlagsSchema Schema { get; }

		public Dictionary<string, ImmutableHashSet<uint>?> MutexSelections { get; } = new();

		public Dictionary<string, TriState> FlagStates { get; } = new();

		MatchMode independentMode = MatchMode.All;

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
				MutexSelections[group.Name] = null;
			foreach (var flag in schema.IndependentFlags)
				FlagStates[flag.Name] = TriState.DontCare;
		}

		public void SetMutexSelection(string groupName, ImmutableHashSet<uint>? values)
		{
			MutexSelections[groupName] = values;
			Revision++;
		}

		public void SetFlagState(string flagName, TriState state)
		{
			FlagStates[flagName] = state;
			Revision++;
		}

		/// <summary>Resets every group to "any" and every flag to don't-care.</summary>
		public void Clear()
		{
			foreach (var key in MutexSelections.Keys.ToArray())
				MutexSelections[key] = null;
			foreach (var key in FlagStates.Keys.ToArray())
				FlagStates[key] = TriState.DontCare;
			IndependentMode = MatchMode.All;
			Revision++;
			Revision++;
		}

		/// <summary>True when no constraints are active — the predicate degenerates to "match all".</summary>
		public bool IsEmpty {
			get {
				foreach (var sel in MutexSelections.Values)
					if (sel is not null)
						return false;
				foreach (var s in FlagStates.Values)
					if (s != TriState.DontCare)
						return false;
				return true;
			}
		}
	}
}
