using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

public abstract partial class HoverableButton : MultiChildView
{
    private readonly Action? _onClick;

    [Observable]
    protected partial bool IsHovered { get; private set; }

    [Observable(true)]
    public partial bool IsEnabled { get; set; }

    protected HoverableButton(Action? onClick = null)
    {
        _onClick = onClick;
        this.UseController(_ => new HoverableButtonController(
            () => { if (IsEnabled) OnClicked(); },
            h => IsHovered = h));
    }

    protected virtual void OnClicked() => _onClick?.Invoke();

    protected void SetBackground(View background) => AddChildToSelf(background);
}
