namespace ZGF.Gui.Desktop.Components.DataGrid;

/// <summary>
/// Implemented by an editor cell's keyboard controller so the grid can hand it focus when an edit begins and
/// take focus back when it ends, without knowing the concrete editor. The grid finds it via
/// <c>InputSystem.GetController(editorView)</c>. The built-in <see cref="DataGridTextEditorController"/>
/// implements it; a custom editor's controller can too.
/// </summary>
public interface IGridCellEditor
{
    /// <summary>Take keyboard focus and ready the editor for input (e.g. select existing text).</summary>
    void BeginEdit();

    /// <summary>Release keyboard focus.</summary>
    void EndEdit();
}
