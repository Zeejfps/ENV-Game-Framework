// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System;
using System.Collections.Generic;
using System.Linq;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// Static description of how a <see cref="FlagsAttribute"/> enum's bits decompose into
	/// mutually-exclusive sub-ranges (mutex groups carved out by <c>*Mask</c> fields) and
	/// independent single-bit flags. The filter UI renders from this shape; the matcher
	/// applies it bitwise.
	/// </summary>
	public sealed record FlagsSchema(
		Type EnumType,
		IReadOnlyList<MutexGroup> MutexGroups,
		IReadOnlyList<IndependentFlag> IndependentFlags)
	{
		/// <summary>What <paramref name="value"/> holds: the value of each mutex group, then which
		/// of the independent flags are set.</summary>
		internal MetadataTooltip Explain(uint value)
		{
			var groups = MutexGroups
				.Select(g => new MetadataFlagGroup(g.Name, true, [new MetadataFlag(g.Label(value & g.Mask), true)]))
				.ToList();
			if (IndependentFlags.Count > 0)
				groups.Add(new MetadataFlagGroup(groups.Count > 0 ? "Flags" : null, false,
					IndependentFlags.Select(f => new MetadataFlag($"{f.Name} ({f.Bit:X4})", (value & f.Bit) != 0)).ToArray()));
			return new MetadataTooltip(null, groups);
		}
	}

	/// <summary>
	/// One sub-range of an enum carved out by a <c>*Mask</c> field. A row's value carries
	/// exactly one of <see cref="Values"/>, isolated by ANDing with <see cref="Mask"/>.
	/// </summary>
	public sealed record MutexGroup(
		string Name,
		uint Mask,
		IReadOnlyList<MutexValue> Values)
	{
		internal string Label(uint value) => Values.FirstOrDefault(v => v.Value == value)?.Label ?? $"Unknown ({value:X4})";
	}

	public sealed record MutexValue(string Label, uint Value);

	/// <summary>
	/// One single-bit flag whose presence is independent of any mutex group. Filtering on
	/// these is tri-state (don't care / required / excluded).
	/// </summary>
	public sealed record IndependentFlag(string Name, uint Bit);
}
