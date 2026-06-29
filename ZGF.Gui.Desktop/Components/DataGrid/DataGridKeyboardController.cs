using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Desktop.Components.DataGrid;

/// <summary>
/// Grid-level keyboard navigation when no cell is being edited: a left press anywhere in the body takes
/// keyboard focus (without consuming the click, so the list still selects), and arrow keys move the
/// selection, Enter/F2 begin editing the current row, and Escape clears the selection. While editing, the
/// editor's own controller owns the keyboard, so this defers (<see cref="DataGridView{TItem}.HandleNavKey"/>
/// no-ops when editing).
/// </summary>
internal sealed class DataGridKeyboardController<TItem> : KeyboardMouseController
{
    private readonly DataGridView<TItem> _grid;
    private readonly InputSystem _input;

    public DataGridKeyboardController(DataGridView<TItem> grid, InputSystem input)
    {
        _grid = grid;
        _input = input;
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase == EventPhase.Bubbling && e.State == InputState.Pressed && e.Button == MouseButton.Left)
            _input.StealFocus(this);
    }

    public override void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
        if (e.State != InputState.Pressed) return;
        if (_grid.HandleNavKey(e.Key)) e.Consume();
    }

    public bool CanReleaseFocus() => true;
}
