using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;

namespace ZGF.Gui.Testing;

/// <summary>Reduces a mounted, laid-out view tree to a <see cref="UiSnapshot"/>. Structural facts
/// (type, bounds, visibility, clip, opacity, text) come straight off the views; role/label/states
/// come from <see cref="View.Accessibility"/>; and live focus/hover are layered in from the
/// <see cref="InputSystem"/> (which the view tree doesn't itself know).</summary>
public static class SnapshotBuilder
{
    public static UiSnapshot Build(View root, InputSystem? input = null)
    {
        var focused = input?.FocusedComponent is { } fc ? input.GetView(fc) : null;
        var hovered = input?.HoveredComponent is { } hc ? input.GetView(hc) : null;
        return new UiSnapshot(BuildNode(root, focused, hovered));
    }

    private static UiNode BuildNode(View view, View? focused, View? hovered)
    {
        var acc = view.Accessibility;
        var text = view is TextView t ? t.Text : null;

        var children = Array.Empty<UiNode>() as IReadOnlyList<UiNode>;
        if (view.IsVisible && view.ChildCount > 0)
        {
            var list = new List<UiNode>(view.ChildCount);
            for (var i = 0; i < view.ChildCount; i++)
                list.Add(BuildNode(view.ChildAt(i), focused, hovered));
            children = list;
        }

        return new UiNode(
            Type: view.GetType().Name,
            Id: view.Id,
            Role: acc.Role,
            Label: ResolveLabel(view, acc, text),
            Text: text,
            Bounds: view.Position,
            Visible: view.IsVisible,
            Opacity: view.Opacity,
            Clips: view.ClipsContent,
            States: ResolveStates(view, acc, focused, hovered),
            Children: children);
    }

    // Explicit label wins; a TextView needs none (its Text carries it); an interactive node with no
    // explicit label takes the aggregate of its descendant text ("Stage All" under a button).
    private static string? ResolveLabel(View view, AccessibilityInfo acc, string? ownText)
    {
        if (acc.Label is { } label) return label;
        if (ownText != null) return null;

        var role = acc.Role;
        if (role == AccessibilityRole.Button || role == AccessibilityRole.ListItem)
            return view.AggregateDescendantText();
        return null;
    }

    private static IReadOnlyList<string> ResolveStates(View view, AccessibilityInfo acc, View? focused, View? hovered)
    {
        var states = new List<string>();
        var s = acc.States;
        if (s.HasFlag(AccessibilityStates.Disabled)) states.Add("disabled");
        if (s.HasFlag(AccessibilityStates.Selected)) states.Add("selected");
        if (s.HasFlag(AccessibilityStates.Checked)) states.Add("checked");
        if (s.HasFlag(AccessibilityStates.Expanded)) states.Add("expanded");
        if (s.HasFlag(AccessibilityStates.Busy)) states.Add("busy");
        if (ReferenceEquals(view, focused)) states.Add("focused");
        if (ReferenceEquals(view, hovered)) states.Add("hovered");
        return states;
    }
}
