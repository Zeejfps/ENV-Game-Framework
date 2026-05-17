using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

public abstract class HoverableButton : MultiChildView
{
    private readonly Action? _onClick;

    protected State<bool> IsHovered { get; } = new(false);

    protected HoverableButton(Action? onClick = null)
    {
        _onClick = onClick;
        Behaviors.Add(new HoverableButtonController(InvokeClick, h => IsHovered.Value = h));
    }

    private void InvokeClick() => OnClicked();

    protected virtual void OnClicked() => _onClick?.Invoke();

    protected void SetBackground(View background) => AddChildToSelf(background);
}
