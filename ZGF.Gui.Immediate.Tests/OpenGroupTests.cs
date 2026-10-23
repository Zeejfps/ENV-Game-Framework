namespace ZGF.Gui.Immediate.Tests;

public sealed class OpenGroupTests
{
    private sealed class Service(string name)
    {
        public string Name { get; } = name;
    }

    private static void EndNothing(object state) { }

    [Fact]
    public void Finding_an_open_group_returns_null_outside_any()
    {
        var found = new UiFrames().Run(0f, ui => ui.FindOpenGroup<Service>());

        Assert.Null(found);
    }

    [Fact]
    public void An_inner_group_is_found_over_the_outer_until_it_is_disposed()
    {
        var seen = new List<string?>();

        new UiFrames(body: ui =>
        {
            using (ui.Group(new Service("outer"), EndNothing))
            {
                seen.Add(ui.FindOpenGroup<Service>()?.Name);
                using (ui.Group(new Service("inner"), EndNothing))
                    seen.Add(ui.FindOpenGroup<Service>()?.Name);
                seen.Add(ui.FindOpenGroup<Service>()?.Name);
            }
            seen.Add(ui.FindOpenGroup<Service>()?.Name);
        }).Frame();

        Assert.Equal(["outer", "inner", "outer", null], seen);
    }

    [Fact]
    public void A_group_left_open_does_not_leak_into_the_next_frame()
    {
        var frames = new UiFrames();
        Assert.Throws<InvalidOperationException>(() => frames.Run(0f, ui => { ui.Group(new Service("leaked"), EndNothing); }));

        var found = frames.Run(0f, ui => ui.FindOpenGroup<Service>());

        Assert.Null(found);
    }
}
