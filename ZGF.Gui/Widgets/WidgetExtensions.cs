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
