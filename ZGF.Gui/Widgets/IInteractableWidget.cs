namespace ZGF.Gui.Widgets;

/// <summary>
/// A widget that is its own interaction target: it builds visuals and exposes the
/// <see cref="IInteractable"/> surface a controller drives. The two collapse into one reference at
/// the attach site, so the consumer writes <c>checkbox.WithController&lt;KbmController&gt;()</c>
/// with no separate target to pass.
/// </summary>
public interface IInteractableWidget : IWidget, IInteractable;
