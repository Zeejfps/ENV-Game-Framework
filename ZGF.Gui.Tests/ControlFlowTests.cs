using ZGF.Gui.Views;
using ZGF.Gui.Widgets;
using ZGF.Observable;

namespace ZGF.Gui.Tests;

/// <summary>
/// Pins the reactive control-flow contract for <see cref="Show"/>/<see cref="Switch{T}"/>:
/// a branch builds and mounts when its discriminator selects it, the outgoing branch unmounts
/// (disposing its bindings) on swap, an unchanged discriminator never rebuilds (memoization),
/// the absent <c>Else</c> hides the host, and <c>KeepAlive</c> keeps visited branches mounted.
/// </summary>
public class ControlFlowTests
{
    private sealed class LifecycleProbe(string name, List<string> log) : IViewBehavior
    {
        public void Attach(View view) => log.Add($"{name}:attach");
        public void Detach(View view) => log.Add($"{name}:detach");
    }

    private sealed record Probe(string Name, List<string> Log) : Widget
    {
        protected override View CreateView(Context ctx)
        {
            Log.Add($"{Name}:build");
            var view = new RectView();
            view.Behaviors.Add(new LifecycleProbe(Name, Log));
            return view;
        }
    }

    [Fact]
    public void Show_True_BuildsAndMountsThen()
    {
        var log = new List<string>();
        var host = (ContainerView)new Show
        {
            When = new State<bool>(true),
            Then = () => new Probe("then", log),
        }.BuildView(new Context());

        host.Mount();

        Assert.Equal(1, host.Children.Count);
        Assert.True(host.IsVisible);
        Assert.Equal(["then:build", "then:attach"], log);
    }

    [Fact]
    public void Show_False_WithoutElse_HidesHostAndBuildsNothing()
    {
        var log = new List<string>();
        var host = (ContainerView)new Show
        {
            When = new State<bool>(false),
            Then = () => new Probe("then", log),
        }.BuildView(new Context());

        host.Mount();

        Assert.Equal(0, host.Children.Count);
        Assert.False(host.IsVisible);
        Assert.Empty(log);
    }

    [Fact]
    public void Show_Toggle_SwapsBranches_AndDisposesOutgoing()
    {
        var log = new List<string>();
        var when = new State<bool>(true);
        var host = (ContainerView)new Show
        {
            When = when,
            Then = () => new Probe("then", log),
            Else = () => new Probe("else", log),
        }.BuildView(new Context());
        host.Mount();
        log.Clear();

        when.Value = false;

        Assert.Equal(1, host.Children.Count);
        Assert.Equal(["then:detach", "else:build", "else:attach"], log);
    }

    [Fact]
    public void Show_UnchangedCondition_DoesNotRebuild()
    {
        var log = new List<string>();
        var when = new State<bool>(true);
        var host = (ContainerView)new Show
        {
            When = when,
            Then = () => new Probe("then", log),
        }.BuildView(new Context());
        host.Mount();
        log.Clear();

        when.Value = true;

        Assert.Empty(log);
    }

    [Fact]
    public void Show_Unmount_DisposesLiveBranch_AndRemountRebuilds()
    {
        var log = new List<string>();
        var host = (ContainerView)new Show
        {
            When = new State<bool>(true),
            Then = () => new Probe("then", log),
        }.BuildView(new Context());

        host.Mount();
        host.Unmount();
        Assert.Equal(0, host.Children.Count);

        log.Clear();
        host.Mount();

        Assert.Equal(1, host.Children.Count);
        Assert.Equal(["then:build", "then:attach"], log);
    }

    [Fact]
    public void Switch_ChangingValue_SwapsCase_AndDisposesOutgoing()
    {
        var log = new List<string>();
        var mode = new State<int>(0);
        var host = (ContainerView)new Switch<int>
        {
            Value = mode,
            Case = m => new Probe($"case{m}", log),
        }.BuildView(new Context());
        host.Mount();
        log.Clear();

        mode.Value = 1;

        Assert.Equal(1, host.Children.Count);
        Assert.Equal(["case0:detach", "case1:build", "case1:attach"], log);
    }

    [Fact]
    public void Switch_KeepAlive_KeepsBranchesMounted_AndDoesNotRebuildOnReturn()
    {
        var log = new List<string>();
        var mode = new State<int>(0);
        var host = (ContainerView)new Switch<int>
        {
            Value = mode,
            KeepAlive = true,
            Case = m => new Probe($"case{m}", log),
        }.BuildView(new Context());
        host.Mount();

        mode.Value = 1;
        mode.Value = 0;

        Assert.Equal(2, host.Children.Count);
        Assert.True(host.Children[0].IsMounted);
        Assert.True(host.Children[1].IsMounted);
        Assert.True(host.Children[0].IsVisible);
        Assert.False(host.Children[1].IsVisible);
        Assert.Equal(
            ["case0:build", "case0:attach", "case1:build", "case1:attach"],
            log);
    }
}
