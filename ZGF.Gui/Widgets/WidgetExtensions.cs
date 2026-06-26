using ZGF.Gui.Bindings;

namespace ZGF.Gui.Widgets;

/// <summary>
/// Lifetime/behavior combinators. Each wraps the widget so the attachment lands on the
/// child's built view — no extra view node is introduced.
/// </summary>
public static class WidgetExtensions
{
    /// <summary>Sets the built view's accessibility role (and optional label), overlaying any
    /// intrinsic value the widget already set. For static semantics — e.g. tagging a row
    /// <see cref="AccessibilityRole.ListItem"/> — that don't change at runtime.</summary>
    public static IWidget WithRole(this IWidget widget, AccessibilityRole role, string? label = null) =>
        new Attachment(widget, v => v.Accessibility = v.Accessibility.Overlay(new AccessibilityInfo(role, label)));

    /// <summary>Binds the built view's accessibility <see cref="AccessibilityStates"/> to a derived
    /// value (auto-tracked), so selection/checked/expanded state stays live in a snapshot. Leaves
    /// role and label untouched.</summary>
    public static IWidget WithAccessibleStates(this IWidget widget, Func<AccessibilityStates> compute) =>
        new Attachment(widget, v => v.BindAccessibilityStates(compute));

    /// <summary>
    /// The built view owns <paramref name="viewModel"/>: disposed when the view unmounts.
    /// Widget-built views are single-mount — bindings close over the instance at build
    /// time, so the view is dead after detach and must not be remounted.
    /// </summary>
    public static IWidget BindVm(this IWidget widget, IDisposable viewModel) =>
        new Attachment(widget, v => v.UseViewModel(() => viewModel, _ => { }));

    /// <summary>Adds behaviors (input controllers, mount hooks, …) to the built view.</summary>
    public static IWidget WithBehaviors(this IWidget widget, params IViewBehavior[] behaviors) =>
        new Attachment(widget, v =>
        {
            foreach (var behavior in behaviors)
                v.Behaviors.Add(behavior);
        });

    /// <summary>
    /// Attaches a view-scoped disposable to the built view: <paramref name="factory"/> runs on
    /// mount with the built view in hand and its result is disposed on unmount. The widget-land
    /// mirror of <see cref="ViewBehaviorExtensions.Use{T}(View, Func{T})"/> — for tooltips, peer
    /// controllers, and other helpers that need the built view but no extra view node.
    /// </summary>
    public static IWidget Use<T>(this IWidget widget, Func<View, T> factory) where T : IDisposable =>
        new Attachment(widget, v => v.Use(() => factory(v)));

    private sealed record Attachment(IWidget Child, Action<View> Attach) : IWidget
    {
        public View BuildView(Context ctx)
        {
            var v = Child.BuildView(ctx);
            Attach(v);
            return v;
        }
    }
}
