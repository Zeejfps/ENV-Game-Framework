using ZGF.Geometry;

namespace ZGF.Gui.Immediate.Tests;

public sealed class FileDropTests
{
    [Fact]
    public void Dropped_files_arrive_on_the_next_frame_where_the_pointer_is_and_only_then()
    {
        var frames = new UiFrames(new DefaultUiState());
        var seen = new List<(IReadOnlyList<string> Files, PointF At)>();
        void Record(Ui ui) => seen.Add((ui.Input.DroppedFiles.ToList(), ui.Input.Pointer));

        frames.MovePointer(40f, 20f);
        frames.DropFiles("a.dll", "b.exe");
        frames.Run(1f / 60f, Record);
        frames.Run(1f / 60f, Record);

        Assert.Equal(["a.dll", "b.exe"], seen[0].Files);
        Assert.Equal(new PointF(40f, 20f), seen[0].At);
        Assert.Empty(seen[1].Files);
    }

    [Fact]
    public void Taking_the_dropped_files_hides_them_from_what_reads_the_input_afterwards()
    {
        var frames = new UiFrames(new DefaultUiState());
        IReadOnlyList<string> taken = [];
        IReadOnlyList<string> left = [];

        frames.DropFiles("a.dll");
        frames.Run(1f / 60f, ui =>
        {
            taken = ui.Input.TakeDroppedFiles();
            left = ui.Input.DroppedFiles.ToList();
        });

        Assert.Equal(["a.dll"], taken);
        Assert.Empty(left);
    }
}
