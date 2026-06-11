namespace ZGF.Gui.Widgets;

/// <summary>
/// Escape hatch: embeds an already-built <see cref="View"/>. Deliberately explicit — a raw view
/// pins the surrounding component to one window, so it should be a visible decision.
/// </summary>
public sealed record Raw : IWidget
{
    public required View View { get; init; }

    public View BuildView(Context ctx) => View;
}