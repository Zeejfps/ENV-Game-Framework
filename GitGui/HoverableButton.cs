using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

public abstract class HoverableButton : MultiChildView
{
    private readonly Action? _onClick;
    private readonly State<bool> _isHovered;
    private readonly State<bool> _isEnabled;

    protected bool IsHovered
    {
        get => _isHovered.Value;
        private set => _isHovered.Value = value;
    }

    public bool IsEnabled
    {
        get => _isEnabled.Value;
        set => _isEnabled.Value = value;
    }

    protected HoverableButton(Action? onClick = null)
    {
        _isHovered = Property(false);
        _isEnabled = Property(true);
        _onClick = onClick;
        this.UseController(_ => new HoverableButtonController(
            () => { if (IsEnabled) OnClicked(); },
            h => IsHovered = h));
    }

    protected virtual void OnClicked() => _onClick?.Invoke();

    protected void SetBackground(View background) => AddChildToSelf(background);
}
