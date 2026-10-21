using ZGF.Desktop;
using ZGF.Observable;

namespace ZGF.Gui.Desktop;

/// <summary>
/// Registers the OS-native <see cref="IFilePicker"/> on a <see cref="Context"/>. The macOS and
/// Linux pickers post their result through the context's <c>IUiDispatcher</c>, resolved on the UI
/// thread when a picker opens, so it only has to be registered by then.
/// </summary>
public static class FilePickerServices
{
    extension(Context context)
    {
        /// <summary>
        /// Registers <see cref="NativeFilePicker.Create"/>'s picker for the current OS, posting
        /// results through the context's <c>IUiDispatcher</c>.
        /// </summary>
        public void AddNativeFilePicker() =>
            context.AddService<IFilePicker>(new UiThreadFilePicker(post => NativeFilePicker.Create(post), () => context.Require<IUiDispatcher>()));
    }
}

/// <summary>Resolves the dispatcher a picker posts its result through on the thread that opens the
/// picker, never on the worker a platform picker waits on.</summary>
internal sealed class UiThreadFilePicker : IFilePicker
{
    private readonly IFilePicker _inner;
    private readonly Func<IUiDispatcher> _resolve;
    private IUiDispatcher? _dispatcher;

    public UiThreadFilePicker(Func<Action<Action>, IFilePicker> create, Func<IUiDispatcher> resolve)
    {
        _resolve = resolve;
        _inner = create(Post);
    }

    public void PickFolder(string title, string? initialDirectory, Action<string> onPicked)
    {
        Resolve();
        _inner.PickFolder(title, initialDirectory, onPicked);
    }

    public void PickFile(string title, string? initialDirectory, IReadOnlyList<FileFilter>? filters, Action<string> onPicked)
    {
        Resolve();
        _inner.PickFile(title, initialDirectory, filters, onPicked);
    }

    public void PickSaveFile(string title, string? initialDirectory, string? suggestedFileName, IReadOnlyList<FileFilter>? filters, Action<string> onPicked)
    {
        Resolve();
        _inner.PickSaveFile(title, initialDirectory, suggestedFileName, filters, onPicked);
    }

    private void Resolve() => _dispatcher ??= _resolve();

    private void Post(Action action) => Volatile.Read(ref _dispatcher)!.Post(action);
}
