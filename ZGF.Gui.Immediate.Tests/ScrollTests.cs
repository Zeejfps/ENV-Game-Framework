namespace ZGF.Gui.Immediate.Tests;

public sealed class ScrollTests
{
    private static void Strip(Ui ui)
    {
        using (ui.Scroll("strip", new Box { Direction = Direction.Horizontal, Gap = 0f }))
        {
            for (var i = 0; i < 10; i++)
                ui.Leaf(ui.GetId(i), new Box { Width = 100f, Height = 30f });
        }
    }

    [Fact]
    public void A_horizontal_scroll_fits_its_contents_height_and_the_wheel_moves_it_sideways()
    {
        var frames = new UiFrames();
        frames.Run(0f, Strip);
        frames.Run(0f, Strip);
        Assert.Equal(30f, frames.RectOf(Strip, ui => ui.GetId("strip")).Height);

        frames.MovePointer(100f, 30f);
        frames.Run(0f, Strip);
        frames.Wheel(-1);
        frames.Run(0f, Strip);
        frames.Run(0f, Strip);

        Assert.Equal(16f - 40f, frames.RectOf(Strip, ui => ui.GetId(0)).X);
    }

    [Fact]
    public void A_horizontal_scroll_stops_at_its_last_item()
    {
        var frames = new UiFrames();
        frames.Run(0f, Strip);
        frames.MovePointer(100f, 30f);
        frames.Run(0f, Strip);
        frames.Wheel(-100);
        frames.Run(0f, Strip);
        frames.Run(0f, Strip);

        Assert.Equal(384f, frames.RectOf(Strip, ui => ui.GetId(9)).Right, 0.01f);
    }

    [Fact]
    public void ScrollIntoView_brings_an_element_into_a_horizontal_scroll()
    {
        var frames = new UiFrames();
        frames.Run(0f, Strip);
        frames.Run(0f, Strip);

        Assert.True(frames.Run(0f, ui =>
        {
            Strip(ui);
            return ui.ScrollIntoView(ui.GetId("strip"), ui.GetId(6));
        }));
        frames.Run(0f, Strip);
        frames.Run(0f, Strip);

        Assert.Equal(384f, frames.RectOf(Strip, ui => ui.GetId(6)).Right, 0.01f);
    }

    [Fact]
    public void ScrollIntoView_brings_an_element_into_a_vertical_scroll()
    {
        var frames = new UiFrames();
        void Column(Ui ui)
        {
            using (ui.Scroll("column", new Box { Gap = 0f }))
            {
                for (var i = 0; i < 40; i++)
                    ui.Leaf(ui.GetId(i), new Box { Width = 10f, Height = 30f });
            }
        }
        frames.Run(0f, Column);
        frames.Run(0f, Column);

        frames.Run(0f, ui =>
        {
            Column(ui);
            ui.ScrollIntoView(ui.GetId("column"), ui.GetId(20));
        });
        frames.Run(0f, Column);
        frames.Run(0f, Column);

        Assert.Equal(284f, frames.RectOf(Column, ui => ui.GetId(20)).Bottom, 0.01f);
    }

    [Fact]
    public void Posted_work_runs_at_the_start_of_the_next_frame()
    {
        var frames = new UiFrames();
        var log = new List<string>();

        frames.Run(0f, ui => ui.Post(() => log.Add("posted")));
        frames.Run(0f, _ => log.Add("render"));

        Assert.Equal(["posted", "render"], log);
    }

    [Fact]
    public void A_picked_file_arrives_on_a_later_frame()
    {
        var frames = new UiFrames();
        frames.FilePicker.Answer = "C:/a.dll";
        string? picked = null;

        frames.Run(0f, ui => ui.PickFile("Open", null, [new ZGF.Desktop.FileFilter("Assemblies", ["*.dll"])], path => picked = path));
        Assert.Null(picked);
        frames.Run(0f, _ => { });

        Assert.Equal("C:/a.dll", picked);
        Assert.Equal(["Open"], frames.FilePicker.Titles);
    }
}
