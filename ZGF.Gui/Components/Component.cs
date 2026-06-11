namespace ZGF.Gui.Components;

/// <summary>
/// Base for composite components: author <see cref="Build"/> returning other components;
/// the recursion into Views happens once, at the window's single BuildView call.
/// Components with mutable state take a ViewModel — the component record itself stays immutable.
/// </summary>
public abstract record Component : IWidget
{
    protected abstract IWidget Build(Context ctx);

    public View BuildView(Context ctx) => Build(ctx).BuildView(ctx);
}