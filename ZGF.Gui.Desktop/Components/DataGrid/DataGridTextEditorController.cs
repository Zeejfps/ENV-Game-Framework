using ZGF.Gui.Desktop.Components.TextInput;
using ZGF.Gui.Desktop.Input;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Desktop.Components.DataGrid;

/// <summary>
/// Keyboard controller for a built-in text editor cell: routes Enter/Up/Down/Tab/Escape to the grid's
/// <see cref="DataGridEditSession"/> (so the focus model moves and commits), and lets the base text-input
/// controller handle ordinary typing, caret motion, and selection. Blur commits.
/// </summary>
public sealed class DataGridTextEditorController : BaseTextInputKbmController, IGridCellEditor
{
    private readonly DataGridEditSession _session;
    private readonly TextInputView _input;

    public DataGridTextEditorController(
        TextInputView input, InputSystem inputSystem, DataGridEditSession session, IClipboard? clipboard = null)
        : base(input, inputSystem, clipboard)
    {
        _input = input;
        _session = session;
        OnTab = () => _session.MoveNext();
        OnShiftTab = () => _session.MovePrev();
    }

    public void BeginEdit()
    {
        BeginEditing();
        _input.SelectAll();
    }

    public void EndEdit() => EndEditing();

    protected override void OnFocusLostCore() => _session.Commit();

    protected override void OnKeyboardKeyPressed(ref KeyboardKeyEvent e)
    {
        switch (e.Key)
        {
            case KeyboardKey.Enter:
            case KeyboardKey.NumpadEnter:
            case KeyboardKey.DownArrow:
                _session.MoveDown();
                e.Consume();
                return;
            case KeyboardKey.UpArrow:
                _session.MoveUp();
                e.Consume();
                return;
            case KeyboardKey.Escape:
                _session.Cancel();
                e.Consume();
                return;
        }

        base.OnKeyboardKeyPressed(ref e);
    }
}
