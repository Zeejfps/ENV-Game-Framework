using ZGF.Gui.Desktop.Components.DataGrid;
using ZGF.Gui.Desktop.Components.TextInput;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Tests;

public class DataGridEditableRowTests
{
    private sealed class Cell { public string A = ""; public string B = ""; }

    private sealed class RecordingEditor : View { public string Value = ""; }

    private static DataGridColumn<Cell> FakeEditable() => new()
    {
        Key = "a",
        Width = ColumnWidth.Flex(),
        Text = c => c.A,
        CreateEditor = _ => new RecordingEditor(),
        BindEditor = (v, item) => ((RecordingEditor)v).Value = item.A,
        CommitEditor = (v, item) => item.A = ((RecordingEditor)v).Value,
    };

    private static DataGridEditableRow<Cell> BuildRow(DataGridColumn<Cell>[] columns)
    {
        var widths = columns.Select(c => c.Width).ToArray();
        return new DataGridEditableRow<Cell>(
            new DataGridColumns(widths), columns, DataGridStyle.Default,
            new FakeCanvas(), new InputSystem(), new DataGridEditSession());
    }

    [Fact]
    public void BuildsEditorCellsForEditableColumns_PreviewForReadOnly()
    {
        var columns = new[] { FakeEditable(), GridColumn.Text<Cell>("b", "B", ColumnWidth.Flex(), c => c.B) };

        var row = BuildRow(columns);

        Assert.True(row.IsEditable(0));
        Assert.NotNull(row.Editor(0));
        Assert.False(row.IsEditable(1));
        Assert.Null(row.Editor(1));
    }

    [Fact]
    public void BindThenCommit_RoundTripsTheEditedValue()
    {
        var columns = new[] { FakeEditable() };
        var row = BuildRow(columns);
        var item = new Cell { A = "original" };

        row.Bind(item);
        var editor = (RecordingEditor)row.Editor(0)!;
        Assert.Equal("original", editor.Value);  // current value loaded in

        editor.Value = "edited";
        row.Commit(item);

        Assert.Equal("edited", item.A);            // edited value written back
    }

    [Fact]
    public void TextEditableColumn_RoundTripsThroughTextInputView()
    {
        var col = GridColumn.TextEditable<Cell>("a", "A", ColumnWidth.Flex(), c => c.A, (c, v) => c.A = v);
        var ctx = new DataGridEditorContext(
            new FakeCanvas(), new InputSystem(), new DataGridEditSession(), DataGridStyle.Default);
        var editor = col.CreateEditor!(ctx);
        var item = new Cell { A = "hi" };

        col.BindEditor!(editor, item);
        ((TextInputView)editor).SetText("bye");
        col.CommitEditor!(editor, item);

        Assert.Equal("bye", item.A);
    }

    [Fact]
    public void TextEditorController_RoutesTabAndShiftTabToTheSession()
    {
        var next = 0;
        var prev = 0;
        var session = new DataGridEditSession { MoveNext = () => next++, MovePrev = () => prev++ };
        var input = new TextInputView(new FakeCanvas());
        var controller = new DataGridTextEditorController(input, new InputSystem(), session);

        controller.OnTab!.Invoke();
        controller.OnShiftTab!.Invoke();

        Assert.Equal(1, next);
        Assert.Equal(1, prev);
    }
}
