namespace ZGF.Gui.Widgets;

/// <summary>
/// A widget that owns a long-lived <typeparamref name="TState"/> object, exposed once it is built.
/// Covariant in <typeparamref name="TState"/> so a <c>StatefulWidget&lt;CheckboxState&gt;</c> can be
/// viewed as an <c>IStatefulWidget&lt;IInteractable&gt;</c> — that up-cast is what lets a single
/// non-generic extension (e.g. <c>WithController&lt;KbmController&gt;()</c>) reach the state without
/// the caller naming its concrete type.
/// <para><see cref="State"/> is only valid after the widget is built; reading it earlier throws.</para>
/// </summary>
public interface IStatefulWidget<out TState> : IWidget where TState : class
{
    /// <summary>The state object, available after <see cref="IWidget.BuildView"/> has run.</summary>
    TState State { get; }
}
