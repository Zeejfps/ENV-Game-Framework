using System.Runtime.InteropServices;

namespace ZGF.Gui.Hosting;

// libxcb, the X protocol library Xlib itself sits on. Unlike Xlib it is thread-safe and reports a
// failed request as an error event rather than through a process-wide error handler, so a
// connection of our own can't take down the app when a requestor window disappears mid-transfer.
internal static unsafe class Xcb
{
    private const string Library = "libxcb.so.1";

    public const byte PropertyNotify = 28;
    public const byte SelectionClear = 29;
    public const byte SelectionRequest = 30;
    public const byte SelectionNotify = 31;
    public const byte SentEventFlag = 0x80;

    public const byte PropModeReplace = 0;
    public const byte PropertyNewValue = 0;
    public const byte PropertyDelete = 1;

    public const uint None = 0;
    public const uint CurrentTime = 0;
    public const uint AnyPropertyType = 0;
    public const uint AtomAtom = 4;
    public const uint AtomString = 31;

    public const ushort WindowClassInputOnly = 2;
    public const uint CwEventMask = 0x800;
    public const uint EventMaskNoEvent = 0;
    public const uint EventMaskPropertyChange = 0x400000;

    [StructLayout(LayoutKind.Sequential)]
    public struct Screen
    {
        public uint Root;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ScreenIterator
    {
        public Screen* Data;
        public int Rem;
        public int Index;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InternAtomReply
    {
        public byte ResponseType;
        public byte Pad0;
        public ushort Sequence;
        public uint Length;
        public uint Atom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GetSelectionOwnerReply
    {
        public byte ResponseType;
        public byte Pad0;
        public ushort Sequence;
        public uint Length;
        public uint Owner;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GetPropertyReply
    {
        public byte ResponseType;
        public byte Format;
        public ushort Sequence;
        public uint Length;
        public uint Type;
        public uint BytesAfter;
        public uint ValueLength;
    }

    /// <summary>An event as xcb hands it over: 32 bytes of the wire event and the full sequence number.</summary>
    [StructLayout(LayoutKind.Sequential, Size = 36)]
    public struct Event
    {
        public byte ResponseType;

        public byte Kind => (byte)(ResponseType & ~SentEventFlag);

        public T As<T>() where T : unmanaged
        {
            fixed (Event* self = &this)
                return *(T*)self;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SelectionRequestEvent
    {
        public byte ResponseType;
        public byte Pad0;
        public ushort Sequence;
        public uint Time;
        public uint Owner;
        public uint Requestor;
        public uint Selection;
        public uint Target;
        public uint Property;
    }

    /// <summary>Sized to the 32 bytes <see cref="xcb_send_event"/> sends.</summary>
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    public struct SelectionNotifyEvent
    {
        public byte ResponseType;
        public byte Pad0;
        public ushort Sequence;
        public uint Time;
        public uint Requestor;
        public uint Selection;
        public uint Target;
        public uint Property;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SelectionClearEvent
    {
        public byte ResponseType;
        public byte Pad0;
        public ushort Sequence;
        public uint Time;
        public uint Owner;
        public uint Selection;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PropertyNotifyEvent
    {
        public byte ResponseType;
        public byte Pad0;
        public ushort Sequence;
        public uint Window;
        public uint Atom;
        public uint Time;
        public byte State;
    }

    [DllImport(Library)]
    public static extern IntPtr xcb_connect(byte* displayName, int* screen);

    [DllImport(Library)]
    public static extern int xcb_connection_has_error(IntPtr connection);

    [DllImport(Library)]
    public static extern void xcb_disconnect(IntPtr connection);

    [DllImport(Library)]
    public static extern int xcb_get_file_descriptor(IntPtr connection);

    [DllImport(Library)]
    public static extern uint xcb_get_maximum_request_length(IntPtr connection);

    [DllImport(Library)]
    public static extern IntPtr xcb_get_setup(IntPtr connection);

    [DllImport(Library)]
    public static extern ScreenIterator xcb_setup_roots_iterator(IntPtr setup);

    [DllImport(Library)]
    public static extern void xcb_screen_next(ScreenIterator* iterator);

    [DllImport(Library)]
    public static extern uint xcb_generate_id(IntPtr connection);

    [DllImport(Library)]
    public static extern uint xcb_create_window(
        IntPtr connection, byte depth, uint window, uint parent, short x, short y, ushort width, ushort height,
        ushort borderWidth, ushort windowClass, uint visual, uint valueMask, uint* valueList);

    [DllImport(Library)]
    public static extern uint xcb_change_window_attributes(IntPtr connection, uint window, uint valueMask, uint* valueList);

    [DllImport(Library)]
    public static extern uint xcb_intern_atom(IntPtr connection, byte onlyIfExists, ushort nameLength, byte* name);

    [DllImport(Library)]
    public static extern InternAtomReply* xcb_intern_atom_reply(IntPtr connection, uint cookie, IntPtr* error);

    [DllImport(Library)]
    public static extern uint xcb_set_selection_owner(IntPtr connection, uint owner, uint selection, uint time);

    [DllImport(Library)]
    public static extern uint xcb_get_selection_owner(IntPtr connection, uint selection);

    [DllImport(Library)]
    public static extern GetSelectionOwnerReply* xcb_get_selection_owner_reply(IntPtr connection, uint cookie, IntPtr* error);

    [DllImport(Library)]
    public static extern uint xcb_convert_selection(IntPtr connection, uint requestor, uint selection, uint target, uint property, uint time);

    [DllImport(Library)]
    public static extern uint xcb_change_property(
        IntPtr connection, byte mode, uint window, uint property, uint type, byte format, uint dataLength, void* data);

    [DllImport(Library)]
    public static extern uint xcb_delete_property(IntPtr connection, uint window, uint property);

    [DllImport(Library)]
    public static extern uint xcb_get_property(
        IntPtr connection, byte delete, uint window, uint property, uint type, uint longOffset, uint longLength);

    [DllImport(Library)]
    public static extern GetPropertyReply* xcb_get_property_reply(IntPtr connection, uint cookie, IntPtr* error);

    [DllImport(Library)]
    public static extern void* xcb_get_property_value(GetPropertyReply* reply);

    [DllImport(Library)]
    public static extern int xcb_get_property_value_length(GetPropertyReply* reply);

    [DllImport(Library)]
    public static extern uint xcb_send_event(IntPtr connection, byte propagate, uint destination, uint eventMask, void* @event);

    [DllImport(Library)]
    public static extern int xcb_flush(IntPtr connection);

    [DllImport(Library)]
    public static extern Event* xcb_poll_for_event(IntPtr connection);
}
