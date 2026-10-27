using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using static ZGF.Gui.Hosting.Xcb;

namespace ZGF.Gui.Hosting;

/// <summary>The CLIPBOARD selection, on an X connection and hidden window of its own: what it offers
/// other apps, and reading what they offer. Used only from the clipboard's thread.</summary>
[SupportedOSPlatform("linux")]
internal sealed unsafe class X11Selection : IDisposable
{
    private const int ReadTimeoutMilliseconds = 1000;
    private const int HandOffTimeoutMilliseconds = 2000;
    private const int TransferPollMilliseconds = 1000;
    private const int NoTimeout = -1;
    private const int NoWakeFd = -1;

    // Larger data goes over INCR in chunks of this size, as GTK and Qt send it, or smaller where
    // the server takes smaller requests.
    private const int MaxChunkBytes = 262144;
    private const int RequestHeaderBytes = 100;
    private const uint WholeProperty = 0x1FFFFFFF;

    private readonly IntPtr _connection;
    private readonly uint _window;
    private readonly X11Atoms _atoms;
    private readonly int _chunkBytes;
    private readonly List<IncrementalTransfer> _transfers = new();
    private ClipboardOffer? _offer;

    private X11Selection(IntPtr connection, int screen)
    {
        _connection = connection;
        _window = CreateWindow(connection, RootOf(connection, screen));
        _atoms = X11Atoms.Intern(connection);
        _chunkBytes = ChunkBytesOf(connection);
        xcb_flush(connection);
    }

    public bool IsBroken => xcb_connection_has_error(_connection) != 0;

    /// <summary>Connects to the display in <c>DISPLAY</c>, or returns null when there is none.</summary>
    public static X11Selection? Connect()
    {
        int screen;
        var connection = xcb_connect(null, &screen);
        if (xcb_connection_has_error(connection) != 0)
        {
            xcb_disconnect(connection);
            return null;
        }
        return new X11Selection(connection, screen);
    }

    private static uint RootOf(IntPtr connection, int screen)
    {
        var iterator = xcb_setup_roots_iterator(xcb_get_setup(connection));
        for (var i = 0; i < screen; i++)
            xcb_screen_next(&iterator);
        return iterator.Data->Root;
    }

    private static uint CreateWindow(IntPtr connection, uint root)
    {
        var window = xcb_generate_id(connection);
        var eventMask = EventMaskPropertyChange;
        xcb_create_window(connection, 0, window, root, 0, 0, 1, 1, 0, WindowClassInputOnly, 0, CwEventMask, &eventMask);
        return window;
    }

    private static int ChunkBytesOf(IntPtr connection)
    {
        var maxRequestBytes = (long)xcb_get_maximum_request_length(connection) * 4;
        return (int)Math.Min(MaxChunkBytes, maxRequestBytes - RequestHeaderBytes);
    }

    /// <summary>Takes the clipboard, offering <paramref name="offer"/> to whoever pastes.</summary>
    public void Offer(ClipboardOffer offer)
    {
        _offer = offer;
        xcb_set_selection_owner(_connection, _window, _atoms.Clipboard, CurrentTime);
        xcb_flush(_connection);
    }

    /// <summary>The clipboard's text: ours while we hold it, else asked of the app holding it.</summary>
    public string? ReadText()
    {
        if (_offer != null)
            return _offer.Text;

        var utf8 = Read(_atoms.Utf8String);
        if (utf8 != null)
            return Encoding.UTF8.GetString(utf8);

        var latin1 = Read(AtomString);
        if (latin1 != null)
            return Encoding.Latin1.GetString(latin1);

        return null;
    }

    private byte[]? Read(uint target)
    {
        xcb_delete_property(_connection, _window, _atoms.ReceivingProperty);
        xcb_convert_selection(_connection, _window, _atoms.Clipboard, target, _atoms.ReceivingProperty, CurrentTime);
        xcb_flush(_connection);

        var deadline = Environment.TickCount64 + ReadTimeoutMilliseconds;
        var notify = WaitFor(e => IsConversionOf(e, target), deadline);
        if (notify == null)
            return null;
        if (notify.Value.As<SelectionNotifyEvent>().Property == None)
            return null;

        var property = TakeProperty(_window, _atoms.ReceivingProperty);
        if (property.Type == _atoms.Incr)
            return ReadIncrementally();
        return property.Data;
    }

    private bool IsConversionOf(Event e, uint target)
    {
        if (e.Kind != SelectionNotify)
            return false;
        var notify = e.As<SelectionNotifyEvent>();
        return notify.Requestor == _window && notify.Target == target;
    }

    private byte[]? ReadIncrementally()
    {
        var data = new MemoryStream();
        while (true)
        {
            var deadline = Environment.TickCount64 + ReadTimeoutMilliseconds;
            if (WaitFor(IsNewChunk, deadline) == null)
                return null;

            var chunk = TakeProperty(_window, _atoms.ReceivingProperty);
            if (chunk.Data.Length == 0)
                return data.ToArray();
            data.Write(chunk.Data);
        }
    }

    private bool IsNewChunk(Event e)
    {
        if (e.Kind != PropertyNotify)
            return false;
        var change = e.As<PropertyNotifyEvent>();
        if (change.State != PropertyNewValue)
            return false;
        return change.Window == _window && change.Atom == _atoms.ReceivingProperty;
    }

    /// <summary>Hands what we offer to the clipboard manager, if one runs, so it can still be pasted
    /// once the app has quit. Serves the manager's requests until it has taken it all.</summary>
    public void HandOffToManager()
    {
        if (_offer == null)
            return;
        if (OwnerOf(_atoms.ClipboardManager) == None)
            return;

        xcb_convert_selection(_connection, _window, _atoms.ClipboardManager, _atoms.SaveTargets, _atoms.ReceivingProperty, CurrentTime);
        xcb_flush(_connection);
        var deadline = Environment.TickCount64 + HandOffTimeoutMilliseconds;
        WaitFor(e => IsConversionOf(e, _atoms.SaveTargets), deadline);
    }

    /// <summary>Handles every event that has arrived, then gives up on stalled transfers.</summary>
    public void HandleEvents()
    {
        var e = NextEvent();
        while (e != null)
        {
            Handle(e.Value);
            e = NextEvent();
        }
        DropStalledTransfers();
        xcb_flush(_connection);
    }

    /// <summary>Blocks until an event arrives, <paramref name="wakeFd"/> is signalled, or a
    /// transfer in progress is due a check for stalling.</summary>
    public void WaitForActivity(int wakeFd)
    {
        var timeout = _transfers.Count > 0 ? TransferPollMilliseconds : NoTimeout;
        Poll(wakeFd, timeout);
    }

    /// <summary>The first event <paramref name="matches"/> accepts, handling the others as they
    /// come; null if none arrives by <paramref name="deadline"/>.</summary>
    private Event? WaitFor(Func<Event, bool> matches, long deadline)
    {
        while (!IsBroken)
        {
            var e = NextEvent();
            if (e == null)
            {
                if (!WaitUntil(deadline))
                    return null;
                continue;
            }
            if (matches(e.Value))
                return e;
            Handle(e.Value);
            xcb_flush(_connection);
        }
        return null;
    }

    private bool WaitUntil(long deadline)
    {
        var remaining = deadline - Environment.TickCount64;
        if (remaining <= 0)
            return false;
        Poll(NoWakeFd, (int)remaining);
        return true;
    }

    // A negative fd is skipped by poll, which is how a wait with no wake fd is expressed.
    private void Poll(int wakeFd, int timeoutMilliseconds)
    {
        var fds = stackalloc Libc.PollFd[2];
        fds[0] = new Libc.PollFd { Fd = xcb_get_file_descriptor(_connection), Events = Libc.PollIn };
        fds[1] = new Libc.PollFd { Fd = wakeFd, Events = Libc.PollIn };
        Libc.poll(fds, 2, timeoutMilliseconds);
    }

    private Event? NextEvent()
    {
        var e = xcb_poll_for_event(_connection);
        if (e == null)
            return null;
        var copy = *e;
        Libc.free(e);
        return copy;
    }

    private void Handle(Event e)
    {
        switch (e.Kind)
        {
            case SelectionRequest:
                Answer(e.As<SelectionRequestEvent>());
                break;
            case SelectionClear:
                LoseOwnership(e.As<SelectionClearEvent>());
                break;
            case PropertyNotify:
                ContinueTransfer(e.As<PropertyNotifyEvent>());
                break;
        }
    }

    private void Answer(SelectionRequestEvent request)
    {
        var property = PropertyFor(request);
        var converted = request.Selection == _atoms.Clipboard && Convert(request.Requestor, request.Target, property);
        var delivered = converted ? property : None;
        Notify(request, delivered);
    }

    // A requestor following the obsolete convention names no property, asking for the target's own.
    private static uint PropertyFor(SelectionRequestEvent request)
    {
        if (request.Property == None)
            return request.Target;
        return request.Property;
    }

    private bool Convert(uint requestor, uint target, uint property)
    {
        if (_offer == null)
            return false;

        if (target == _atoms.Targets)
        {
            WriteAtoms(requestor, property, AtomAtom, OfferedTargets(_offer));
            return true;
        }
        if (target == _atoms.Multiple)
            return ConvertMultiple(requestor, property);
        if (target == _atoms.SaveTargets)
        {
            WriteAtoms(requestor, property, _atoms.Null, []);
            return true;
        }

        var data = DataFor(_offer, target);
        if (data == null)
            return false;
        Write(requestor, property, TypeFor(target), data);
        return true;
    }

    private uint[] OfferedTargets(ClipboardOffer offer)
    {
        var targets = new List<uint>
        {
            _atoms.Targets, _atoms.Multiple, _atoms.Utf8String, _atoms.PlainTextUtf8, _atoms.Text, AtomString,
        };
        if (offer.Html != null)
            targets.Add(_atoms.Html);
        return targets.ToArray();
    }

    private byte[]? DataFor(ClipboardOffer offer, uint target)
    {
        if (target == _atoms.Utf8String)
            return offer.Utf8Text;
        if (target == _atoms.PlainTextUtf8)
            return offer.Utf8Text;
        if (target == _atoms.Text)
            return offer.Utf8Text;
        if (target == AtomString)
            return offer.Latin1Text;
        if (target == _atoms.Html)
            return offer.Html;
        return null;
    }

    // TEXT asks for text in whatever encoding the owner picks; the reply says which.
    private uint TypeFor(uint target)
    {
        if (target == _atoms.Text)
            return _atoms.Utf8String;
        return target;
    }

    /// <summary>Converts each target the requestor's property pairs with a property, marking the
    /// pairs that failed by setting their property to None.</summary>
    private bool ConvertMultiple(uint requestor, uint property)
    {
        var pairs = GetProperty(requestor, property, delete: false);
        var atoms = MemoryMarshal.Cast<byte, uint>(pairs.Data).ToArray();
        for (var i = 0; i + 1 < atoms.Length; i += 2)
        {
            if (!ConvertPair(requestor, atoms[i], atoms[i + 1]))
                atoms[i + 1] = None;
        }
        WriteAtoms(requestor, property, _atoms.AtomPair, atoms);
        return true;
    }

    private bool ConvertPair(uint requestor, uint target, uint property)
    {
        if (target == _atoms.Multiple)
            return false;
        return Convert(requestor, target, property);
    }

    private void Notify(SelectionRequestEvent request, uint property)
    {
        var notify = new SelectionNotifyEvent
        {
            ResponseType = SelectionNotify,
            Time = request.Time,
            Requestor = request.Requestor,
            Selection = request.Selection,
            Target = request.Target,
            Property = property,
        };
        xcb_send_event(_connection, 0, request.Requestor, EventMaskNoEvent, &notify);
    }

    private void WriteAtoms(uint window, uint property, uint type, uint[] atoms)
    {
        fixed (uint* data = atoms)
            xcb_change_property(_connection, PropModeReplace, window, property, type, 32, (uint)atoms.Length, data);
    }

    private void Write(uint requestor, uint property, uint type, byte[] data)
    {
        if (data.Length > _chunkBytes)
        {
            StartTransfer(requestor, property, type, data);
            return;
        }
        WriteBytes(requestor, property, type, data);
    }

    private void WriteBytes(uint window, uint property, uint type, ReadOnlySpan<byte> data)
    {
        fixed (byte* bytes = data)
            xcb_change_property(_connection, PropModeReplace, window, property, type, 8, (uint)data.Length, bytes);
    }

    // Watches the requestor's properties first, so its deletion of the INCR announcement, which
    // asks for the first chunk, is not missed.
    private void StartTransfer(uint requestor, uint property, uint type, byte[] data)
    {
        WatchProperties(requestor, EventMaskPropertyChange);
        var transfer = new IncrementalTransfer(requestor, property, type, data, Environment.TickCount64);
        var length = (uint)transfer.Length;
        xcb_change_property(_connection, PropModeReplace, requestor, property, _atoms.Incr, 32, 1, &length);
        _transfers.Add(transfer);
    }

    private void ContinueTransfer(PropertyNotifyEvent change)
    {
        if (change.State != PropertyDelete)
            return;
        var transfer = _transfers.Find(t => t.Feeds(change.Window, change.Atom));
        if (transfer == null)
            return;

        var chunk = transfer.TakeChunk(_chunkBytes, Environment.TickCount64);
        WriteBytes(transfer.Requestor, transfer.Property, transfer.Type, chunk.Span);
        if (transfer.IsDone)
            EndTransfer(transfer);
    }

    private void DropStalledTransfers()
    {
        var now = Environment.TickCount64;
        var stalled = _transfers
            .Where(t => t.IsStalled(now))
            .ToList();
        foreach (var transfer in stalled)
            EndTransfer(transfer);
    }

    private void EndTransfer(IncrementalTransfer transfer)
    {
        _transfers.Remove(transfer);
        if (_transfers.Exists(t => t.Requestor == transfer.Requestor))
            return;
        WatchProperties(transfer.Requestor, EventMaskNoEvent);
    }

    private void WatchProperties(uint window, uint eventMask)
    {
        xcb_change_window_attributes(_connection, window, CwEventMask, &eventMask);
    }

    // A clear can arrive after the app took the clipboard back, from the app that held it in
    // between; only a clear that leaves another owner in place ends what we offer.
    private void LoseOwnership(SelectionClearEvent clear)
    {
        if (clear.Selection != _atoms.Clipboard)
            return;
        if (OwnerOf(_atoms.Clipboard) == _window)
            return;
        _offer = null;
    }

    private uint OwnerOf(uint selection)
    {
        var cookie = xcb_get_selection_owner(_connection, selection);
        var reply = xcb_get_selection_owner_reply(_connection, cookie, null);
        if (reply == null)
            return None;
        var owner = reply->Owner;
        Libc.free(reply);
        return owner;
    }

    private X11Property TakeProperty(uint window, uint property) => GetProperty(window, property, delete: true);

    private X11Property GetProperty(uint window, uint property, bool delete)
    {
        byte deleteFlag = delete ? (byte)1 : (byte)0;
        var cookie = xcb_get_property(_connection, deleteFlag, window, property, AnyPropertyType, 0, WholeProperty);
        var reply = xcb_get_property_reply(_connection, cookie, null);
        if (reply == null)
            return new X11Property(None, []);

        var length = xcb_get_property_value_length(reply);
        var data = new ReadOnlySpan<byte>(xcb_get_property_value(reply), length).ToArray();
        var type = reply->Type;
        Libc.free(reply);
        return new X11Property(type, data);
    }

    public void Dispose() => xcb_disconnect(_connection);

    private readonly record struct X11Property(uint Type, byte[] Data);
}
