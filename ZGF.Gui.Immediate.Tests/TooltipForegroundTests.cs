using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class TooltipForegroundTests
{
    private const string Tip = "explains it";
    private readonly UiFrames _frames = new(width: 800f, height: 600f, body: Body);

    private static void Body(Ui ui)
    {
        var target = ui.Interact("target");
        ui.Leaf(target.Id, new Box { Width = 100f, Height = 30f });
        ui.Tooltip(target, Tip);
    }

    private void Hover() => _frames.Frame().MovePointer(50f, 15f).Frame();

    [Fact]
    public void A_tooltip_does_not_open_while_another_app_has_the_focus()
    {
        Hover();
        _frames.Background().Frame();
        _frames.Wait(1).Frame(2);

        Assert.False(_frames.Drew(Tip));
    }

    [Fact]
    public void An_open_tooltip_closes_when_the_app_loses_the_focus()
    {
        Hover();
        _frames.Wait(1).Frame(2);
        Assert.True(_frames.Drew(Tip));

        _frames.Background().Frame(2);

        Assert.False(_frames.Drew(Tip));
    }

    [Fact]
    public void Back_in_front_a_tooltip_waits_its_delay_again()
    {
        Hover();
        _frames.Background().Frame();
        _frames.Wait(1).Foreground().Frame(2);
        Assert.False(_frames.Drew(Tip));

        _frames.Wait(1).Frame(2);

        Assert.True(_frames.Drew(Tip));
    }
}
