using ZGF.Gui.Immediate.Widgets;

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
        var frames = new UiFrames();

        var found = frames.Run(0f, ui => ui.Get<Service>());

        Assert.Null(found);
    }

    [Fact]
    public void An_inner_provide_overrides_the_outer_until_it_is_disposed()
    {
        var frames = new UiFrames();
        var seen = new List<string?>();

        frames.Run(0f, ui =>
        {
            using (ui.Provide(new Service("outer")))
            {
                seen.Add(ui.Get<Service>()?.Name);
                using (ui.Provide(new Service("inner")))
                    seen.Add(ui.Get<Service>()?.Name);
                seen.Add(ui.Get<Service>()?.Name);
            }
            seen.Add(ui.Get<Service>()?.Name);
        });

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

    [Fact]
    public void Widgets_fall_back_to_the_default_theme_and_follow_a_provided_one()
    {
        var frames = new UiFrames();
        var custom = WidgetTheme.Default with { CornerRadius = 2f };

        var (fallback, provided) = frames.Run(0f, ui =>
        {
            var before = WidgetTheme.For(ui);
            using (ui.Provide(custom))
                return (before, WidgetTheme.For(ui));
        });

        Assert.Same(WidgetTheme.Default, fallback);
        Assert.Same(custom, provided);
    }
}
