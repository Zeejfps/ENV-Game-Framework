using System.Text;
using ZGF.Gui.Views;

namespace ZGF.Gui.Desktop.Inspection;

/// <summary>Tree queries over a mounted, laid-out <see cref="View"/> subtree. The <c>Find*</c>
/// helpers consider the receiver itself; <see cref="Descendants"/> does not.</summary>
public static class ViewQueryExtensions
{
    public static IEnumerable<View> Descendants(this View view)
    {
        for (var i = 0; i < view.ChildCount; i++)
        {
            var child = view.ChildAt(i);
            yield return child;
            foreach (var descendant in child.Descendants())
                yield return descendant;
        }
    }

    public static IEnumerable<View> SelfAndDescendants(this View view)
    {
        yield return view;
        foreach (var descendant in view.Descendants())
            yield return descendant;
    }

    public static View? Find(this View view, Func<View, bool> predicate)
    {
        foreach (var candidate in view.SelfAndDescendants())
            if (predicate(candidate))
                return candidate;
        return null;
    }

    public static IEnumerable<View> FindAll(this View view, Func<View, bool> predicate) =>
        view.SelfAndDescendants().Where(predicate);

    public static View? FindById(this View view, string id) => view.Find(v => v.Id == id);

    public static IEnumerable<View> FindAllById(this View view, string id) => view.FindAll(v => v.Id == id);

    public static T? FindByType<T>(this View view) where T : View => (T?)view.Find(v => v is T);

    public static IEnumerable<T> FindAllByType<T>(this View view) where T : View =>
        view.SelfAndDescendants().OfType<T>();

    public static View? FindByText(this View view, string text) =>
        view.Find(v => v is TextView t && t.Text == text);

    /// <summary>Finds a text view by content. <paramref name="exact"/> false matches a
    /// case-insensitive substring — the way an LLM tends to refer to on-screen text.</summary>
    public static View? FindByText(this View view, string text, bool exact) =>
        exact
            ? view.FindByText(text)
            : view.Find(v => v is TextView { Text: { } t } &&
                             t.Contains(text, StringComparison.OrdinalIgnoreCase));

    public static View? FindByRole(this View view, AccessibilityRole role) =>
        view.Find(v => v.Accessibility.Role == role);

    public static IEnumerable<View> FindAllByRole(this View view, AccessibilityRole role) =>
        view.FindAll(v => v.Accessibility.Role == role);

    /// <summary>Finds a clickable (<see cref="AccessibilityRole.Button"/>) view by its accessible
    /// label — exact (case-insensitive) by default, or substring when <paramref name="exact"/> is
    /// false. This is how an action phrased by intent ("click Push") resolves to a view.</summary>
    public static View? FindClickable(this View view, string label, bool exact = true) =>
        view.FindAllByRole(AccessibilityRole.Button)
            .FirstOrDefault(v => NameMatches(v.AccessibleName(), label, exact));

    /// <summary>The accessible name used to match a view in queries and snapshots: the explicit
    /// <see cref="AccessibilityInfo.Label"/>, else a <see cref="TextView"/>'s own text, else the
    /// aggregate of descendant text (an icon+label button reads as its label).</summary>
    public static string? AccessibleName(this View view)
    {
        if (view.Accessibility.Label is { } label) return label;
        if (view is TextView t) return t.Text;
        return view.AggregateDescendantText();
    }

    internal static string? AggregateDescendantText(this View view)
    {
        var sb = new StringBuilder();
        CollectText(view, sb);
        var s = sb.ToString().Trim();
        return s.Length == 0 ? null : s;
    }

    private static void CollectText(View view, StringBuilder sb)
    {
        for (var i = 0; i < view.ChildCount; i++)
        {
            var child = view.ChildAt(i);
            if (!child.IsVisible) continue;
            if (child is TextView { Text: { } text } && text.Length > 0)
                AppendReadable(sb, text);
            CollectText(child, sb);
        }
    }

    // Appends the readable part of a run, skipping Private-Use-Area codepoints — an icon font (Lucide
    // etc.) maps glyphs into the PUA (U+E000..U+F8FF), so an icon+label button reads "Push", not
    // "<glyph> Push".
    private static void AppendReadable(StringBuilder sb, string text)
    {
        Span<char> buf = text.Length <= 256 ? stackalloc char[text.Length] : new char[text.Length];
        var n = 0;
        foreach (var ch in text)
            if (ch < 0xE000 || ch > 0xF8FF)
                buf[n++] = ch;
        if (n == 0) return;
        if (sb.Length > 0 && sb[^1] != ' ') sb.Append(' ');
        sb.Append(buf[..n]);
    }

    private static bool NameMatches(string? name, string query, bool exact)
    {
        if (name == null) return false;
        return exact
            ? string.Equals(name, query, StringComparison.OrdinalIgnoreCase)
            : name.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
