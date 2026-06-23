using ZGF.Gui.Views;

namespace ZGF.Gui.Testing;

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
}
