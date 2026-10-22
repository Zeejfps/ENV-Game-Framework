using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class TooltipForegroundTests
{
    private const string Tip = "explains it";
    private readonly UiFrames _frames = new(width: 800f, height: 600f);

    private static void Body(Ui ui)
    {
        var target = ui.Interact("target");
        ui.Leaf(target.Id, new Box { Width = 100f, Height = 30f });
        ui.Tooltip(target, Tip);
    }

    private void Run(int frames = 1)
    {
        for (var i = 0; i < frames; i++)
            _frames.Run(0f, Body);
    }

    private void Hover()
    {
        Run();
        _frames.MovePointer(50f, 15f);
        Run();
    }

    [Fact]
    public void A_tooltip_does_not_open_while_another_app_has_the_focus()
    {
        Hover();
        _frames.Background();
        Run();
        _frames.Wait(1);
        Run(2);

        Assert.False(_frames.Drew(Tip));
    }

    [Fact]
    public void An_open_tooltip_closes_when_the_app_loses_the_focus()
    {
        Hover();
        _frames.Wait(1);
        Run(2);
        Assert.True(_frames.Drew(Tip));

        _frames.Background();
        Run(2);

        Assert.False(_frames.Drew(Tip));
    }

    [Fact]
    public void Back_in_front_a_tooltip_waits_its_delay_again()
    {
        Hover();
        _frames.Background();
        Run();
        _frames.Wait(1);
        _frames.Foreground();
        Run(2);
        Assert.False(_frames.Drew(Tip));

        _frames.Wait(1);
        Run(2);

        Assert.True(_frames.Drew(Tip));
    }
}
