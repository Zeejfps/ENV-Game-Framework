using ZGF.Desktop.Backends;
using ZGF.Desktop.Sharing;

namespace ZGF.Desktop;

public static class NativeFileSharer
{
    /// <summary>
    /// The share sheet for the current OS: Windows' share UI, else one that cannot share
    /// (<see cref="IFileSharer.CanShare"/> false). Its sheets belong to <paramref name="owner"/>, and
    /// it hands its outcomes to <paramref name="postToUiThread"/>, which must run them on the UI
    /// thread.
    /// </summary>
    public static IFileSharer Create(Action<Action> postToUiThread, IWindow? owner)
    {
        if (OperatingSystem.IsWindows() && owner is GlfwWindowBase window && window.NativeHandle != IntPtr.Zero
            && WindowsFileSharer.IsAvailable())
            return new WindowsFileSharer(window.NativeHandle, postToUiThread);
        return new NoFileSharer();
    }
}

/// <summary>The <see cref="IFileSharer"/> of an OS with no share sheet for files.</summary>
public sealed class NoFileSharer : IFileSharer
{
    public bool CanShare => false;

    public void Share(FileToShare file, Action<ShareOutcome> onDone) =>
        onDone(new ShareOutcome.Failed("There is no share sheet for files on this OS"));
}
