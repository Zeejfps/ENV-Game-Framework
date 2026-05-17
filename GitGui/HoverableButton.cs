using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

public abstract class HoverableButton : MultiChildView
{
    private readonly Action? _onClick;

    protected State<bool> IsHovered { get; } = new(false);
    public State<bool> IsEnabled { get; } = new(true);

    protected HoverableButton(Action? onClick = null)
    {
        _onClick = onClick;
        Behaviors.Add(new HoverableButtonController(
            () => { if (IsEnabled) OnClicked(); },
            h => IsHovered.Set(h)));
    }

    protected virtual void OnClicked() => _onClick?.Invoke();

    protected void SetBackground(View background) => AddChildToSelf(background);
}
