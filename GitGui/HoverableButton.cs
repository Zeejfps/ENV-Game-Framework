using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

public abstract class HoverableButton : MultiChildView
{
    private readonly Action? _onClick;

    protected State<bool> IsHovered { get; } = new(false);
    public State<bool> IsEnabled { get; } = new(true);
    public State<ICommand?> Command { get; } = new(null);

    protected HoverableButton(Action? onClick = null, string? tooltip = null)
    {
        _onClick = onClick;
        this.UseController(_ => new HoverableButtonController(
            () => { if (IsEnabled) OnClicked(); },
            h => IsHovered.Set(h)));

        if (!string.IsNullOrEmpty(tooltip))
        {
            this.UsePresenter(ctx => new Tooltip(this, ctx, tooltip, IsHovered, IsEnabled));
        }
    }

    protected virtual void OnClicked()
    {
        if (Command.Value is { } cmd) cmd.Execute();
        else _onClick?.Invoke();
    }

    public void BindCommand(ICommand command)
    {
        Command.Value = command;
        IsEnabled.BindTo(command.CanExecute);
    }

    protected void SetBackground(View background) => AddChildToSelf(background);
}
