using ZGF.Gui;
using ZGF.Gui.Desktop;

namespace ZGF.Gui.Prototype.Components;

/// <summary>
/// A window-agnostic, immutable description of UI. <see cref="BuildView"/> turns it into a
/// retained <see cref="View"/> wired against one window's <see cref="Context"/>. Components are
/// shareable and rebuildable; built Views belong to the window whose context built them.
/// </summary>
public interface IComponent
{
    View BuildView(Context ctx);
}

/// <summary>
/// Base for composite components: author <see cref="Build"/> returning other components;
/// the recursion into Views happens once, at the window's single BuildView call.
/// Components with mutable state take a ViewModel — the component record itself stays immutable.
/// </summary>
public abstract record Component : IComponent
{
    protected abstract IComponent Build(Context ctx);

    public View BuildView(Context ctx) => Build(ctx).BuildView(ctx);
}

/// <summary>
/// Escape hatch: embeds an already-built <see cref="View"/>. Deliberately explicit — a raw view
/// pins the surrounding component to one window, so it should be a visible decision.
/// </summary>
public sealed record Raw : IComponent
{
    public required View View { get; init; }

    public View BuildView(Context ctx) => View;
}

public static class ComponentAppExtensions
{
    /// <summary>Mounts a component as the main window's root content.</summary>
    public static GuiAppBuilder UseContent(this GuiAppBuilder builder, IComponent root) =>
        builder.UseContent(root.BuildView);
}
