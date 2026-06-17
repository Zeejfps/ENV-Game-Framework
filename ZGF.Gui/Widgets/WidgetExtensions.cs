using ZGF.Observable;

namespace ZGF.Gui.Widgets;

/// <summary>
/// Lifetime/behavior combinators. Each wraps the widget so the attachment lands on the
/// child's built view — no extra view node is introduced.
/// </summary>
public static class WidgetExtensions
{
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

    /// <summary>
    /// Materializes <paramref name="prop"/> into <paramref name="state"/> with the built view's
    /// mount lifetime — the prop's binding (constant, observable, or compute) drives the state and
    /// tears down on unmount. A <see cref="Prop{T}"/> can flow <em>into</em> a view but can't be
    /// read back; use this when a widget needs to <em>read</em> a consumer-supplied prop inside its
    /// own compute (e.g. an icon color that depends on both a bound badge count and local state).
    /// </summary>
    public static IWidget Materialize<T>(this IWidget widget, Context ctx, Prop<T> prop, State<T> state) =>
        new Attachment(widget, v => prop.Apply(ctx, v, (_, value) => state.Value = value));

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
