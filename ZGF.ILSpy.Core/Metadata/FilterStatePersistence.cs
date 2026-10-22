// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace ZGF.ILSpy.Core.Metadata
{
	/// <summary>
	/// Round-trips a <see cref="FilterState"/> through an <see cref="XElement"/> so the
	/// schema-driven flag-filter dropdown's user-set state survives a session restart.
	/// Storage is keyed externally (by page-entry type + column name); this class only
	/// owns the per-state XML shape.
	/// </summary>
	/// <remarks>
	/// Wire shape (only non-default constraints are written):
	/// <code>
	///   &lt;FilterState&gt;
	///     &lt;Mutex group="VisibilityMask"&gt;
	///       &lt;Value&gt;2&lt;/Value&gt;
	///       &lt;Value&gt;4&lt;/Value&gt;
	///     &lt;/Mutex&gt;
	///     &lt;Flag name="Sealed" state="Required" /&gt;
	///     &lt;IndependentMode&gt;Any&lt;/IndependentMode&gt;
	///   &lt;/FilterState&gt;
	/// </code>
	/// Apply tolerates unknown group/flag names so an older saved XML can't break a newer
	/// schema (the offending entry is silently skipped, others still apply).
	/// </remarks>
	public static class FilterStatePersistence
	{
		const string Root = "FilterState";
		const string MutexElement = "Mutex";
		const string FlagElement = "Flag";
		const string ValueElement = "Value";
		const string IndependentModeElement = "IndependentMode";

		public static XElement ToXml(FilterState state)
		{
			ArgumentNullException.ThrowIfNull(state);
			var element = new XElement(Root);
			foreach (var (groupName, selection) in state.MutexSelections)
			{
				if (selection is null)
					continue;       // "any" — write nothing
				element.Add(new XElement(MutexElement,
					new XAttribute("group", groupName),
					selection.OrderBy(v => v).Select(v =>
						new XElement(ValueElement, v.ToString(CultureInfo.InvariantCulture)))));
			}
			foreach (var (flagName, triState) in state.FlagStates)
			{
				if (triState == TriState.DontCare)
					continue;       // default — write nothing
				element.Add(new XElement(FlagElement,
					new XAttribute("name", flagName),
					new XAttribute("state", triState.ToString())));
			}
			if (state.IndependentMode != MatchMode.All)
				element.Add(new XElement(IndependentModeElement, state.IndependentMode.ToString()));
			return element;
		}

		public static void ApplyXml(FilterState state, XElement xml)
		{
			ArgumentNullException.ThrowIfNull(state);
			ArgumentNullException.ThrowIfNull(xml);
			foreach (var mutex in xml.Elements(MutexElement))
			{
				var groupName = (string?)mutex.Attribute("group");
				if (groupName is null || !state.MutexSelections.ContainsKey(groupName))
					continue;       // schema drift: group no longer present
				var values = mutex.Elements(ValueElement)
					.Select(v => uint.TryParse(v.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : (uint?)null)
					.Where(v => v.HasValue)
					.Select(v => v!.Value)
					.ToImmutableHashSet();
				state.SetMutexSelection(groupName, values.IsEmpty ? null : values);
			}
			foreach (var flag in xml.Elements(FlagElement))
			{
				var flagName = (string?)flag.Attribute("name");
				if (flagName is null || !state.FlagStates.ContainsKey(flagName))
					continue;       // schema drift: flag no longer present
				if (Enum.TryParse<TriState>((string?)flag.Attribute("state"), out var tri))
					state.SetFlagState(flagName, tri);
			}
			if (Enum.TryParse<MatchMode>(xml.Element(IndependentModeElement)?.Value, out var mode))
				state.IndependentMode = mode;
		}
	}
}
