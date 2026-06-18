using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

/// <summary>
/// Makes <see cref="Value"/> resolvable from the build <see cref="Context"/> of <see cref="Child"/>
/// and its whole subtree: the child builds against a scoped context that resolves
/// <typeparamref name="T"/> to it. The widget-land mirror of the child-<see cref="Context"/>
/// scoping controllers use — a context provider for ambient values a subtree reads via
/// <see cref="Prop.Deferred{T}"/> (the same shape as <c>Theme.Color</c>).
/// </summary>
public sealed record Provide<T> : Widget where T : class
{
    public required T Value { get; init; }
    public required IWidget Child { get; init; }

    protected override View CreateView(Context ctx)
    {
        var scope = new Context(ctx);
        scope.AddService(Value);
        return Child.BuildView(scope);
    }
}
