using ZGF.Gui.Widgets;

namespace ZGF.Gui.Desktop.Widgets;

/// <summary>
/// An input surface that owns an <see cref="Interaction"/> and publishes it to its subtree's
/// <see cref="Context"/>, so descendant style props resolve against interaction flags without the
/// authoring widget creating or pumping any state. The state-driven counterpart to a bare
/// <see cref="KbmInput"/>: pair it with <c>(theme, state) =&gt; value</c> resolvers (see GitBench's
/// <c>Theme.Color((s, st) =&gt; …)</c>) the way Flutter pairs a button with <c>WidgetStateProperty</c>.
/// </summary>
public sealed record Interactive : Widget
{
    public required IWidget Child { get; init; }
    public Action? OnClick { get; init; }

    protected override View CreateView(Context ctx)
    {
        var interaction = new Interaction();
        var scope = new Context(ctx);
        scope.AddService(interaction);

        IWidget input = new KbmInput
        {
            OnClick = OnClick,
            OnHoverEnter = () => interaction.SetHovered(true),
            OnHoverExit = () => interaction.SetHovered(false),
            Child = Child,
        };

        return input.BuildView(scope);
    }
}
