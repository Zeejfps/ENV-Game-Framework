// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System;
using System.Collections.Generic;

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
		IReadOnlyList<IndependentFlag> IndependentFlags);

	/// <summary>
	/// One sub-range of an enum carved out by a <c>*Mask</c> field. A row's value carries
	/// exactly one of <see cref="Values"/>, isolated by ANDing with <see cref="Mask"/>.
	/// </summary>
	public sealed record MutexGroup(
		string Name,
		uint Mask,
		IReadOnlyList<MutexValue> Values);

	public sealed record MutexValue(string Label, uint Value);

	/// <summary>
	/// One single-bit flag whose presence is independent of any mutex group. Filtering on
	/// these is tri-state (don't care / required / excluded).
	/// </summary>
	public sealed record IndependentFlag(string Name, uint Bit);
}
