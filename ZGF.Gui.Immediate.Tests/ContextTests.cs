namespace ZGF.Gui.Immediate.Tests;

public sealed class ContextTests
{
    private sealed class Service(string name)
    {
        public string Name { get; } = name;
    }

    [Fact]
    public void Get_returns_null_outside_any_provide()
    {
        var found = new UiFrames().Run(0f, ui => ui.Get<Service>());

        Assert.Null(found);
    }

    [Fact]
    public void An_inner_provide_overrides_the_outer_until_it_is_disposed()
    {
        var seen = new List<string?>();

        new UiFrames(body: ui =>
        {
            using (ui.Provide(new Service("outer")))
            {
                seen.Add(ui.Get<Service>()?.Name);
                using (ui.Provide(new Service("inner")))
                    seen.Add(ui.Get<Service>()?.Name);
                seen.Add(ui.Get<Service>()?.Name);
            }
            seen.Add(ui.Get<Service>()?.Name);
        }).Frame();

        Assert.Equal(["outer", "inner", "outer", null], seen);
    }

    [Fact]
    public void Provided_values_do_not_leak_into_the_next_frame()
    {
        var frames = new UiFrames();
        Assert.Throws<InvalidOperationException>(() => frames.Run(0f, ui => { ui.Provide(new Service("leaked")); }));

        var found = frames.Run(0f, ui => ui.Get<Service>());

        Assert.Null(found);
    }
}
