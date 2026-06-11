namespace ZGF.Gui.Components;

/// <summary>
/// A window-agnostic, immutable description of UI. <see cref="BuildView"/> turns it into a
/// retained <see cref="View"/> wired against one window's <see cref="Context"/>. Components are
/// shareable and rebuildable; built Views belong to the window whose context built them.
/// </summary>
public interface IWidget
{
    View BuildView(Context ctx);
}

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

/// <summary>
/// Escape hatch: embeds an already-built <see cref="View"/>. Deliberately explicit — a raw view
/// pins the surrounding component to one window, so it should be a visible decision.
/// </summary>
public sealed record Raw : IWidget
{
    public required View View { get; init; }

    public View BuildView(Context ctx) => View;
}
