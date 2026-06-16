using ZGF.Gui.Bindings;
using ZGF.Gui.Views;
using ZGF.Observable;

namespace ZGF.Gui.Widgets;

/// <summary>
/// Inference-friendly entry point for <see cref="Each{T}"/> — constructors can't infer
/// generic arguments, so <c>Each.Of(vm.Tasks, new TaskRow(), gap: 4)</c> beats
/// <c>new Each&lt;TaskViewModel&gt;(...)</c>. Rarer props via <c>with</c>:
/// <c>Each.Of(...) with { CrossAxis = ... }</c>.
/// </summary>
public static class Each
{
    public static Each<T> Of<T>(
        ObservableList<T> items,
        IWidget template,
        float gap = 0f,
        Axis axis = Axis.Vertical) where T : class =>
        new() { Items = items, Template = template, Gap = gap, ListAxis = axis };
}

/// <summary>
/// Dynamic children: mirrors an <see cref="ObservableList{T}"/> into a flex container.
/// Each item gets a scoped child <see cref="Context"/> with the item registered as a service,
/// and <paramref name="Template"/> is built against that scope — so item components resolve
/// their item VM from the context like every other dependency, never via constructor.
/// The parent <paramref name="ctx"/> is captured for late item builds — safe because the
/// built view is already pinned to this window.
/// </summary>
public sealed record Each<T> : FlexBase
    where T : class
{
    public required ObservableList<T> Items { get; init; }
    public required IWidget Template { get; init; }
    public Axis ListAxis { get; init; } = ZGF.Gui.Views.Axis.Vertical;

    /// <summary>
    /// Optional extra per-item registrations on the item's scope (beyond the item itself) —
    /// e.g. a row controller or per-item service. Singletons the scope creates are disposed
    /// when the item is removed from the list.
    /// </summary>
    public Action<Context, T>? ConfigureScope { get; init; }

    protected override Axis Axis => ListAxis;

    protected override View CreateView(Context ctx)
    {
        var v = (FlexView)base.CreateView(ctx);
        var scopes = new Dictionary<View, Context>();
        v.Children.BindChildren(Items,
            item =>
            {
                var scope = new Context(ctx);
                scope.AddService(item);
                ConfigureScope?.Invoke(scope, item);
                var child = Template.BuildView(scope);
                scopes[child] = scope;
                return child;
            },
            onRemoved: child =>
            {
                if (scopes.Remove(child, out var scope))
                    scope.Dispose();
            });
        return v;
    }
}