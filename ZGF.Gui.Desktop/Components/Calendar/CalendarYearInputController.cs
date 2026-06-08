using ZGF.Gui.Desktop.Components.TextInput;
using ZGF.Gui.Desktop.Input;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Desktop.Components.Calendar;

public sealed class CalendarYearInputController : BaseTextInputKbmController
{
    private readonly TextInputView _input;
    private readonly Action<string> _onCommit;
    private readonly Action _onRevert;
    private bool _reverting;

    public CalendarYearInputController(TextInputView input, Action<string> onCommit, Action onRevert)
        : base(input)
    {
        _input = input;
        _onCommit = onCommit;
        _onRevert = onRevert;
    }

    protected override void OnKeyboardKeyPressed(ref KeyboardKeyEvent e)
    {
        if (e.Key is KeyboardKey.Enter or KeyboardKey.NumpadEnter)
        {
            EndEditing();
            e.Consume();
            return;
        }

        if (e.Key == KeyboardKey.Escape)
        {
            _reverting = true;
            EndEditing();
            e.Consume();
            return;
        }

        base.OnKeyboardKeyPressed(ref e);
    }

    public override void OnFocusLost()
    {
        if (_reverting)
        {
            _reverting = false;
            _onRevert();
        }
        else
        {
            _onCommit(_input.Text.ToString());
        }

        base.OnFocusLost();
    }
}
