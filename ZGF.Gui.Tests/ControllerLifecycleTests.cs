using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

/// <summary>
/// Pins controller registration to the mounted lifetime: a controller registers with its
/// window's InputSystem on mount, unregisters on unmount, and factory-created controllers
/// are recreated per mount and disposed per unmount.
/// </summary>
public class ControllerLifecycleTests
{
    private sealed class StubController : KeyboardMouseController, IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    [Fact]
    public void Controller_RegistersOnMount_UnregistersOnUnmount()
    {
        var input = new InputSystem();
        var view = new RectView();
        var controller = new StubController();
        view.UseController(input, controller);

        Assert.Null(input.GetController(view));

        view.Mount();
        Assert.Same(controller, input.GetController(view));

        view.Unmount();
        Assert.Null(input.GetController(view));
    }

    [Fact]
    public void RemovingRow_UnregistersItsController()
    {
        var input = new InputSystem();
        var row = new RectView();
        row.UseController(input, new StubController());
        var parent = new RectView { Children = { row } };
        parent.Mount();

        Assert.NotNull(input.GetController(row));

        parent.Children.Remove(row);
        Assert.Null(input.GetController(row));
    }

    [Fact]
    public void FactoryController_RecreatedPerMount_DisposedPerUnmount()
    {
        var input = new InputSystem();
        var view = new RectView();
        var instances = new List<StubController>();
        view.UseController(input, () =>
        {
            var c = new StubController();
            instances.Add(c);
            return c;
        });

        view.Mount();
        view.Unmount();
        view.Mount();

        Assert.Equal(2, instances.Count);
        Assert.True(instances[0].IsDisposed);
        Assert.False(instances[1].IsDisposed);
        Assert.Same(instances[1], input.GetController(view));
    }

    [Fact]
    public void InstanceController_NotDisposedByUnmount()
    {
        var input = new InputSystem();
        var view = new RectView();
        var controller = new StubController();
        view.UseController(input, controller);

        view.Mount();
        view.Unmount();

        Assert.False(controller.IsDisposed);
    }
}
