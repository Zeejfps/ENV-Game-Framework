// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// Builds a <see cref="FlagsSchema"/> for a <see cref="FlagsAttribute"/> enum by
	/// partitioning its public-static fields against the <c>*Mask</c> fields the runtime
	/// libraries use to mark mutually-exclusive sub-ranges (<c>VisibilityMask</c>,
	/// <c>LayoutMask</c>, <c>StringFormatMask</c>, <c>MemberAccessMask</c>, …). Cached
	/// per type for the lifetime of the process.
	/// </summary>
	public static class FlagsSchemaInferer
	{
		static readonly ConcurrentDictionary<Type, FlagsSchema> cache = new();

		/// <summary>
		/// Bits that exist in ECMA-335 but have no member in the runtime's enum, so the
		/// field walk cannot surface them. Appended to the inferred independent flags.
		/// </summary>
		static readonly Dictionary<Type, IndependentFlag[]> extraFlags = new() {
			// ECMA-335 II.23.1.15 Forwarder: set on ExportedType rows that forward the
			// type to another assembly.
			[typeof(TypeAttributes)] = [new IndependentFlag("IsTypeForwarder", 0x00200000)],
		};

		/// <summary>Masks that gather independent flags rather than mutually exclusive values.</summary>
		static readonly HashSet<string> flagMasks = ["ReservedMask", "SpecialConstraintMask"];

		/// <summary>Whether <paramref name="type"/> is an enum of bits: one marked
		/// <see cref="FlagsAttribute"/>, or one whose <c>*Mask</c> fields carve its bits up, as
		/// <see cref="MethodImplAttributes"/> does.</summary>
		public static bool IsFlags(Type type) =>
			type.IsEnum && (type.IsDefined(typeof(FlagsAttribute), false)
				|| type.GetFields(BindingFlags.Public | BindingFlags.Static).Any(static f => IsMask(f.Name)));

		public static FlagsSchema For(Type enumType)
		{
			ArgumentNullException.ThrowIfNull(enumType);
			if (!enumType.IsEnum)
				throw new ArgumentException($"Type '{enumType}' is not an enum.", nameof(enumType));
			return cache.GetOrAdd(enumType, Build);
		}

		static FlagsSchema Build(Type enumType)
		{
			var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
			var maskFields = new List<(string Name, uint Mask)>();
			var valueFields = new List<(string Name, uint Value)>();
			// A zero-valued member fits every mask, so value alone cannot attribute it to a
			// group. ECMA-335 and the reflection enums declare each mask followed directly by
			// its members (VisibilityMask, NotPublic, Public, ...), so a zero member is
			// claimed as the zero label of the most recently declared mask. First wins; a zero
			// member no mask claims (None) labels every group left without one.
			var zeroNames = new Dictionary<uint, string>();
			uint? currentMask = null;
			string? unclaimedZero = null;
			foreach (var f in fields)
			{
				uint v = ToUInt32(f.GetRawConstantValue());
				if (IsMask(f.Name))
				{
					if (flagMasks.Contains(f.Name))
						continue;
					maskFields.Add((f.Name, v));
					currentMask = v;
				}
				else
				{
					valueFields.Add((f.Name, v));
					if (v == 0 && currentMask is uint mask && !zeroNames.ContainsKey(mask))
						zeroNames[mask] = f.Name;
					else if (v == 0)
						unclaimedZero ??= f.Name;
				}
			}

			// Sort masks ascending by mask value so that when a value fits in more than one
			// mask (e.g. value 0 fits all of them), the smallest one wins. This keeps the
			// partition deterministic and prefers the most specific axis.
			maskFields.Sort((a, b) => a.Mask.CompareTo(b.Mask));

			// First pass: bucket every non-zero value into the first mask it fits inside.
			// Zero-value fields were already claimed (or not) as group zero labels above;
			// each mutex group always carries a zero option regardless.
			var groupBuckets = maskFields.ToDictionary(m => m.Mask,
				m => new List<MutexValue>());
			var independentFlags = new List<IndependentFlag>();
			var seenValues = new HashSet<uint>();
			foreach (var (name, value) in valueFields)
			{
				if (value == 0)
					continue; // zero defaults are synthesised below
				if (!seenValues.Add(value))
					continue; // skip aliases (multiple names for the same bits)
				bool placed = false;
				foreach (var (maskName, mask) in maskFields)
				{
					if ((value & ~mask) != 0)
						continue;
					groupBuckets[mask].Add(new MutexValue($"{ValueName(name, maskName)} ({value:X4})", value));
					placed = true;
					break;
				}
				if (!placed && BitOperations.IsPow2(value))
					independentFlags.Add(new IndependentFlag(name, value));
			}

			if (extraFlags.TryGetValue(enumType, out var extras))
				independentFlags.AddRange(extras);
			independentFlags.Sort(static (a, b) => a.Bit.CompareTo(b.Bit));

			// Build the final mutex groups. Drop empty buckets — a *Mask field whose
			// values weren't named in the enum (or were aliased away) doesn't earn a
			// dropdown section. Each group leads with its zero entry: the enum's declared
			// zero member (NotPublic, AutoLayout, None, ...), or a synthesised "(none)" for
			// an enum without one.
			var groups = new List<MutexGroup>();
			foreach (var (maskName, mask) in maskFields)
			{
				var values = groupBuckets[mask];
				if (values.Count == 0)
					continue;
				values.Sort(static (a, b) => a.Value.CompareTo(b.Value));
				var zeroName = zeroNames.GetValueOrDefault(mask) ?? unclaimedZero;
				values.Insert(0, zeroName != null
					? new MutexValue($"{ValueName(zeroName, maskName)} (0000)", 0)
					: new MutexValue("(none)", 0));
				groups.Add(new MutexGroup(GroupName(maskName), mask, values));
			}

			return new FlagsSchema(enumType, groups, independentFlags);
		}

		static bool IsMask(string name) => name.EndsWith("Mask", StringComparison.Ordinal);

		static string GroupName(string maskName) => maskName[..^"Mask".Length];

		/// <summary>A value's name without its group's prefix: <c>Ansi</c> of <c>CharSetAnsi</c>.</summary>
		static string ValueName(string name, string maskName)
		{
			var group = GroupName(maskName);
			return name.Length > group.Length && name.StartsWith(group, StringComparison.Ordinal) ? name[group.Length..] : name;
		}

		static uint ToUInt32(object? raw)
		{
			if (raw is null)
				return 0;
			return Convert.ToUInt32(Convert.ToInt64(raw, CultureInfo.InvariantCulture)
				& 0xFFFFFFFFL, CultureInfo.InvariantCulture);
		}
	}
}
