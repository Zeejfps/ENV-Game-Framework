// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using ICSharpCode.Decompiler.Util;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// Rich cell tooltip for an enum-valued metadata column: it breaks a flags value down into
	/// per-bit groups, showing each named flag with a checkbox reflecting whether the bit is set
	/// (multiple-choice groups) or the single selected member of a mutually exclusive sub-range
	/// (single-choice groups). Entries build one with a collection initializer of
	/// <see cref="FlagGroup"/>s; <see cref="MetadataGrid"/> hands it on as a <see cref="MetadataTooltip"/>.
	/// </summary>
	internal sealed class FlagsTooltip : IEnumerable<FlagGroup>
	{
		// The (value, flagsType) ctor parameters are unused but kept so entry call sites read the
		// same as the value they describe; the groups carry the actual selection state.
		public FlagsTooltip(int value = 0, Type? flagsType = null)
		{
		}

		public List<FlagGroup> Groups { get; } = new List<FlagGroup>();

		public void Add(FlagGroup group) => Groups.Add(group);

		public IEnumerator<FlagGroup> GetEnumerator() => Groups.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => Groups.GetEnumerator();

		/// <summary>
		/// Tooltip for a <see cref="TypeAttributes"/> column: one single-choice group per
		/// mutually exclusive sub-range (visibility, layout, class semantics, string format,
		/// custom format) plus a multiple-choice group for the remaining independent bits.
		/// Shared by every table whose rows carry type attributes (TypeDef, ExportedType).
		/// </summary>
		public static FlagsTooltip ForTypeAttributes(TypeAttributes attributes)
		{
			const TypeAttributes otherFlagsMask = ~(TypeAttributes.VisibilityMask | TypeAttributes.LayoutMask | TypeAttributes.ClassSemanticsMask | TypeAttributes.StringFormatMask | TypeAttributes.CustomFormatMask);
			// ECMA-335 II.23.1.15 Forwarder: set on ExportedType rows that forward the type to
			// another assembly. System.Reflection.TypeAttributes has no member for the bit, so
			// the enum-driven GetFlags enumeration cannot surface it; inject it explicitly.
			const TypeAttributes isTypeForwarder = (TypeAttributes)0x00200000;
			return new FlagsTooltip {
				FlagGroup.CreateSingleChoiceGroup(typeof(TypeAttributes), "Visibility: ", (int)TypeAttributes.VisibilityMask, (int)(attributes & TypeAttributes.VisibilityMask), new Flag("NotPublic (0000)", 0, false), includeAny: false),
				FlagGroup.CreateSingleChoiceGroup(typeof(TypeAttributes), "Class layout: ", (int)TypeAttributes.LayoutMask, (int)(attributes & TypeAttributes.LayoutMask), new Flag("AutoLayout (0000)", 0, false), includeAny: false),
				FlagGroup.CreateSingleChoiceGroup(typeof(TypeAttributes), "Class semantics: ", (int)TypeAttributes.ClassSemanticsMask, (int)(attributes & TypeAttributes.ClassSemanticsMask), new Flag("Class (0000)", 0, false), includeAny: false),
				FlagGroup.CreateSingleChoiceGroup(typeof(TypeAttributes), "String format: ", (int)TypeAttributes.StringFormatMask, (int)(attributes & TypeAttributes.StringFormatMask), new Flag("AnsiClass (0000)", 0, false), includeAny: false),
				FlagGroup.CreateSingleChoiceGroup(typeof(TypeAttributes), "Custom format: ", (int)TypeAttributes.CustomFormatMask, (int)(attributes & TypeAttributes.CustomFormatMask), new Flag("Value0 (0000)", 0, false), includeAny: false),
				new MultipleChoiceGroup(
					FlagGroup.GetFlags(typeof(TypeAttributes), (int)otherFlagsMask, (int)(attributes & otherFlagsMask))
						.Append(new Flag($"IsTypeForwarder ({(int)isTypeForwarder:X4})", (int)isTypeForwarder, (attributes & isTypeForwarder) != 0))) {
					Header = "Flags:",
					SelectedFlags = (int)(attributes & otherFlagsMask),
				},
			};
		}
	}

	internal readonly struct Flag
	{
		public string Name { get; }
		public int Value { get; }
		public bool IsSelected { get; }

		public Flag(string name, int value, bool isSelected)
		{
			this.Name = name;
			this.Value = value;
			this.IsSelected = isSelected;
		}
	}

	internal abstract class FlagGroup
	{
		public static MultipleChoiceGroup CreateMultipleChoiceGroup(Type flagsType, string? header = null, int mask = -1, int selectedValue = 0, bool includeAll = true)
		{
			return new MultipleChoiceGroup(GetFlags(flagsType, mask, selectedValue, includeAll ? "<All>" : null)) {
				Header = header,
				SelectedFlags = selectedValue,
			};
		}

		public static SingleChoiceGroup CreateSingleChoiceGroup(Type flagsType, string? header = null, int mask = -1, int selectedValue = 0, Flag defaultFlag = default, bool includeAny = true)
		{
			var group = new SingleChoiceGroup(GetFlags(flagsType, mask, selectedValue, includeAny ? "<Any>" : null)) {
				Header = header,
			};
			group.SelectedFlag = group.Flags.SingleOrDefault(f => f.Value == selectedValue);
			if (group.SelectedFlag.Name == null)
				group.SelectedFlag = defaultFlag;
			return group;
		}

		public static IEnumerable<Flag> GetFlags(Type flagsType, int mask = -1, int selectedValues = 0, string? neutralItem = null)
		{
			if (neutralItem != null)
				yield return new Flag(neutralItem, -1, false);

			foreach (var item in flagsType.GetFields(BindingFlags.Static | BindingFlags.Public))
			{
				if (item.Name.EndsWith("Mask", StringComparison.Ordinal))
					continue;
				int value = (int)CSharpPrimitiveCast.Cast(TypeCode.Int32, item.GetRawConstantValue(), false);
				if ((value & mask) == 0)
					continue;
				yield return new Flag($"{item.Name} ({value:X4})", value, (selectedValues & value) != 0);
			}
		}

		public string? Header { get; set; }

		public IList<Flag> Flags { get; protected set; } = Array.Empty<Flag>();
	}

	internal sealed class MultipleChoiceGroup : FlagGroup
	{
		public MultipleChoiceGroup(IEnumerable<Flag> flags)
		{
			this.Flags = flags.ToList();
		}

		public int SelectedFlags { get; set; }
	}

	internal sealed class SingleChoiceGroup : FlagGroup
	{
		public SingleChoiceGroup(IEnumerable<Flag> flags)
		{
			this.Flags = flags.ToList();
		}

		public Flag SelectedFlag { get; set; }
	}
}
