// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// Evaluation-friendly snapshot of a <see cref="FilterState"/>. Compile once per state
	/// change, then call <see cref="Matches"/> per row. The mutex checks normalise each
	/// group's bits to a 0-base index so the allowed set is at most 64 entries (covers
	/// every standard CLR flag enum).
	/// </summary>
	public sealed record CompiledFilter(
		uint RequiredMask,
		uint ExcludedMask,
		MatchMode IndependentMode,
		IReadOnlyList<MutexCheck> MutexChecks)
	{
		/// <summary>The empty filter — short-circuits to "match every row".</summary>
		public static readonly CompiledFilter Empty = new(
			RequiredMask: 0,
			ExcludedMask: 0,
			IndependentMode: MatchMode.All,
			MutexChecks: System.Array.Empty<MutexCheck>());

		public bool IsEmpty => RequiredMask == 0 && ExcludedMask == 0 && MutexChecks.Count == 0;

		public static CompiledFilter Compile(FilterState state)
		{
			uint required = 0;
			uint excluded = 0;
			foreach (var (name, tri) in state.FlagStates)
			{
				if (tri == TriState.DontCare)
					continue;
				// Look up the flag's bit. Independent flags are unique by name in the
				// schema, so a linear scan is fine — schemas are at most a couple dozen
				// entries long.
				uint bit = 0;
				foreach (var f in state.Schema.IndependentFlags)
				{
					if (f.Name == name)
					{
						bit = f.Bit;
						break;
					}
				}
				if (bit == 0)
					continue;
				if (tri == TriState.Required)
					required |= bit;
				else
					excluded |= bit;
			}

			var checks = new List<MutexCheck>();
			foreach (var group in state.Schema.MutexGroups)
			{
				if (!state.MutexSelections.TryGetValue(group.Name, out var selection)
					|| selection is null)
					continue; // "any" — no constraint on this axis
				int shift = BitOperations.TrailingZeroCount(group.Mask);
				uint normalisedMask = group.Mask >> shift;
				if (normalisedMask < 64)
				{
					ulong allowed = 0;
					foreach (var v in selection)
						allowed |= 1UL << (int)((v & group.Mask) >> shift);
					checks.Add(new MutexCheck(group.Mask, shift, allowed, AllowedSet: null));
				}
				else
				{
					var set = ImmutableHashSet.CreateRange(selection);
					checks.Add(new MutexCheck(group.Mask, shift, AllowedBitmap: 0, AllowedSet: set));
				}
			}

			return new CompiledFilter(required, excluded, state.IndependentMode, checks);
		}

		public bool Matches(uint attrs)
		{
			if ((attrs & ExcludedMask) != 0)
				return false;
			foreach (var c in MutexChecks)
			{
				uint v = (attrs & c.Mask) >> c.Shift;
				bool ok = c.AllowedSet is { } set
					? set.Contains((attrs & c.Mask))
					: (c.AllowedBitmap & (1UL << (int)v)) != 0;
				if (!ok)
					return false;
			}
			if (RequiredMask != 0)
			{
				bool ok = IndependentMode == MatchMode.All
					? (attrs & RequiredMask) == RequiredMask
					: (attrs & RequiredMask) != 0;
				if (!ok)
					return false;
			}
			return true;
		}
	}

	public sealed record MutexCheck(
		uint Mask,
		int Shift,
		ulong AllowedBitmap,
		ImmutableHashSet<uint>? AllowedSet);
}
