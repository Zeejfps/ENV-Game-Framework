using System.Collections.Concurrent;
using System.Runtime.Versioning;

namespace ZGF.Gui.Hosting;

// The X11 clipboard, on a connection of its own rather than GLFW's: GLFW answers every selection
// request on its connection with its own plain-text clipboard. A thread of its own serves other
// apps' pastes even while the app's loop sleeps waiting for input; calls from the app are posted
// to it in order, so a read after a copy sees the copy.
[SupportedOSPlatform("linux")]
internal sealed unsafe class X11Clipboard : PlatformClipboard
{
    // Long enough for both conversions a read tries to time out on the clipboard's thread.
    private const int ReadWaitMilliseconds = 3000;
    private const int StopWaitMilliseconds = 5000;

    private readonly X11Selection _selection;
    private readonly ConcurrentQueue<Action> _posted = new();
    private readonly int _wakeFd;
    private readonly Thread _thread;
    private bool _stopping;
    private volatile bool _stopped;

    private X11Clipboard(X11Selection selection)
    {
        _selection = selection;
        _wakeFd = Libc.eventfd(0, Libc.EventFdCloseOnExec | Libc.EventFdNonBlocking);
        _thread = new Thread(Run) { IsBackground = true, Name = "X11 clipboard" };
        _thread.Start();
    }

    /// <summary>Connects to the X display, or returns null when there is none to connect to.</summary>
    public static X11Clipboard? Connect()
    {
        var selection = ConnectSelection();
        if (selection == null)
            return null;
        return new X11Clipboard(selection);
    }

    private static X11Selection? ConnectSelection()
    {
        try
        {
            return X11Selection.Connect();
        }
        catch (DllNotFoundException)
        {
            return null;
        }
    }

    public override void SetText(string text) => Offer(new ClipboardOffer(text, null));

    public override void SetHtml(string html, string text) => Offer(new ClipboardOffer(text, html));

    private void Offer(ClipboardOffer offer) => Post(() => _selection.Offer(offer));

    public override string? GetText()
    {
        if (_stopped)
            return null;
        var read = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(() => read.SetResult(_selection.ReadText()));
        if (!read.Task.Wait(ReadWaitMilliseconds))
            return null;
        return read.Task.Result;
    }

    public override void Dispose()
    {
        Post(Stop);
        if (_thread.Join(StopWaitMilliseconds))
            Libc.close(_wakeFd);
    }

    private void Stop() => _stopping = true;

    private void Post(Action work)
    {
        _posted.Enqueue(work);
        Wake();
    }

    private void Wake()
    {
        ulong increment = 1;
        Libc.write(_wakeFd, &increment, sizeof(ulong));
    }

    private void Run()
    {
        while (true)
        {
            RunPosted();
            if (_stopping)
                break;
            if (_selection.IsBroken)
                break;
            _selection.HandleEvents();
            _selection.WaitForActivity(_wakeFd);
            ClearWake();
        }
        if (_stopping)
            _selection.HandOffToManager();
        _selection.Dispose();
        _stopped = true;
    }

    private void RunPosted()
    {
        while (!_stopping && _posted.TryDequeue(out var work))
            work();
    }

    private void ClearWake()
    {
        ulong count;
        Libc.read(_wakeFd, &count, sizeof(ulong));
    }
}
