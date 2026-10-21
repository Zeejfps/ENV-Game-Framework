using ZGF.Desktop;
using ZGF.Gui.Desktop;
using ZGF.Observable;

namespace ZGF.Gui.Tests;

public sealed class FilePickerDispatcherTests
{
    private sealed class WorkerPicker(Action<Action> post) : IFilePicker
    {
        public void PickFolder(string title, string? initialDirectory, Action<string> onPicked) => Answer(onPicked);

        public void PickFile(string title, string? initialDirectory, IReadOnlyList<FileFilter>? filters, Action<string> onPicked) => Answer(onPicked);

        public void PickSaveFile(string title, string? initialDirectory, string? suggestedFileName, IReadOnlyList<FileFilter>? filters, Action<string> onPicked) =>
            Answer(onPicked);

        private void Answer(Action<string> onPicked) => Task.Run(() => post(() => onPicked("picked"))).Wait();
    }

    private sealed class Dispatcher : IUiDispatcher
    {
        public readonly List<Action> Posted = new();

        public void Post(Action action)
        {
            lock (Posted) Posted.Add(action);
        }
    }

    [Fact]
    public void The_dispatcher_is_resolved_on_the_thread_that_opens_the_picker()
    {
        var dispatcher = new Dispatcher();
        var resolvedOn = -1;
        var picker = new UiThreadFilePicker(post => new WorkerPicker(post), () =>
        {
            resolvedOn = Environment.CurrentManagedThreadId;
            return dispatcher;
        });
        string? picked = null;

        picker.PickFile("Open", null, null, path => picked = path);

        Assert.Equal(Environment.CurrentManagedThreadId, resolvedOn);
        var post = Assert.Single(dispatcher.Posted);
        Assert.Null(picked);
        post();
        Assert.Equal("picked", picked);
    }

    [Fact]
    public void The_dispatcher_only_has_to_be_registered_by_the_time_a_picker_opens()
    {
        var context = new Context();
        context.AddNativeFilePicker();
        var dispatcher = new Dispatcher();
        context.AddService<IUiDispatcher>(dispatcher);

        Assert.NotNull(context.Require<IFilePicker>());
    }
}
