using System.Collections.Concurrent;

namespace ZGF.Gui.Immediate;

public abstract partial class Ui
{
    private readonly IUiState _state;

    /// <summary>The theme of the UI state at this point of the frame.</summary>
    public UiTheme Theme => _state.Theme;
}

/// <summary>A <see cref="Ui"/> carrying the app's <typeparamref name="T"/>, what its views read
/// besides the app's own state: the theme, and whatever else the app puts there.</summary>
public sealed class Ui<T> : Ui where T : class, IUiState
{
    internal Ui(T state, ICanvas canvas, IClipboard clipboard, IPopupHost? popupHost = null, ConcurrentQueue<Action>? posted = null)
        : base(state, canvas, clipboard, popupHost, posted)
    {
        State = state;
    }

    public T State { get; }
}
