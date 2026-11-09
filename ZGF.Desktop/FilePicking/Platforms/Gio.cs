using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ZGF.Desktop.FilePicking;

/// <summary>
/// The slice of GLib/GIO's D-Bus and GVariant C API that <see cref="PortalFilePicker"/> needs.
/// GIO is present wherever an XDG desktop portal is, as the portal itself is built on it.
/// </summary>
/// <remarks>
/// Ownership follows GLib: the <c>g_variant_new_*</c> constructors return floating references that
/// the container or call they are passed to takes over; <c>get_child_value</c>,
/// <c>lookup_value</c> and <c>call_sync</c> return references the caller must
/// <see cref="g_variant_unref"/>; <c>get_string</c> borrows from its variant.
/// </remarks>
[SupportedOSPlatform("linux")]
internal static unsafe class Gio
{
    private const string LibGio = "libgio-2.0.so.0";
    private const string LibGlib = "libglib-2.0.so.0";
    private const string LibGobject = "libgobject-2.0.so.0";

    public const int G_BUS_TYPE_SESSION = 2;
    public const int G_DBUS_CALL_FLAGS_NONE = 0;
    public const int G_DBUS_SIGNAL_FLAGS_NONE = 0;
    /// <summary>The bus's default method call timeout (25 s).</summary>
    public const int DefaultTimeout = -1;

    /// <summary>GLib's <c>GError</c>.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct GError
    {
        public uint Domain;
        public int Code;
        public IntPtr Message;
    }

    // --- Connection and calls ---

    [DllImport(LibGio)]
    public static extern IntPtr g_bus_get_sync(int busType, IntPtr cancellable, out GError* error);

    [DllImport(LibGio)]
    public static extern IntPtr g_dbus_connection_get_unique_name(IntPtr connection);

    [DllImport(LibGio)]
    public static extern IntPtr g_dbus_connection_call_sync(
        IntPtr connection,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string busName,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string objectPath,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string interfaceName,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string methodName,
        IntPtr parameters,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string replyType,
        int flags,
        int timeoutMs,
        IntPtr cancellable,
        out GError* error);

    [DllImport(LibGio)]
    public static extern uint g_dbus_connection_signal_subscribe(
        IntPtr connection,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? sender,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? interfaceName,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? member,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? objectPath,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? arg0,
        int flags,
        delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, void> callback,
        IntPtr userData,
        IntPtr userDataFreeFunc);

    [DllImport(LibGio)]
    public static extern void g_dbus_connection_signal_unsubscribe(IntPtr connection, uint subscriptionId);

    [DllImport(LibGobject)]
    public static extern void g_object_unref(IntPtr obj);

    [DllImport(LibGlib)]
    public static extern void g_error_free(GError* error);

    // --- Main context: signals are delivered while the subscribing thread iterates its context ---

    [DllImport(LibGlib)]
    public static extern IntPtr g_main_context_new();

    [DllImport(LibGlib)]
    public static extern void g_main_context_push_thread_default(IntPtr context);

    [DllImport(LibGlib)]
    public static extern void g_main_context_pop_thread_default(IntPtr context);

    [DllImport(LibGlib)]
    public static extern int g_main_context_iteration(IntPtr context, int mayBlock);

    [DllImport(LibGlib)]
    public static extern void g_main_context_unref(IntPtr context);

    // --- GVariant construction ---

    [DllImport(LibGlib)]
    public static extern IntPtr g_variant_new_string([MarshalAs(UnmanagedType.LPUTF8Str)] string value);

    [DllImport(LibGlib)]
    public static extern IntPtr g_variant_new_boolean(int value);

    [DllImport(LibGlib)]
    public static extern IntPtr g_variant_new_uint32(uint value);

    /// <summary>An <c>ay</c> holding the string's UTF-8 bytes and its nul terminator.</summary>
    [DllImport(LibGlib)]
    public static extern IntPtr g_variant_new_bytestring([MarshalAs(UnmanagedType.LPUTF8Str)] string value);

    [DllImport(LibGlib)]
    public static extern IntPtr g_variant_new_variant(IntPtr value);

    [DllImport(LibGlib)]
    public static extern IntPtr g_variant_new_dict_entry(IntPtr key, IntPtr value);

    [DllImport(LibGlib)]
    public static extern IntPtr g_variant_new_tuple(IntPtr[] children, nuint count);

    /// <summary>Children must be non-empty: the element type is taken from them.</summary>
    [DllImport(LibGlib)]
    public static extern IntPtr g_variant_new_array(IntPtr childType, IntPtr[] children, nuint count);

    // --- GVariant reading ---

    [DllImport(LibGlib)]
    public static extern nuint g_variant_n_children(IntPtr value);

    [DllImport(LibGlib)]
    public static extern IntPtr g_variant_get_child_value(IntPtr value, nuint index);

    [DllImport(LibGlib)]
    public static extern uint g_variant_get_uint32(IntPtr value);

    /// <summary>The value inside a <c>v</c>.</summary>
    [DllImport(LibGlib)]
    public static extern IntPtr g_variant_get_variant(IntPtr value);

    [DllImport(LibGlib)]
    public static extern IntPtr g_variant_get_string(IntPtr value, IntPtr length);

    /// <summary>The value under <paramref name="key"/> in an <c>a{sv}</c> when it has
    /// <paramref name="expectedType"/>, else null.</summary>
    [DllImport(LibGlib)]
    public static extern IntPtr g_variant_lookup_value(
        IntPtr dictionary,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string key,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string expectedType);

    [DllImport(LibGlib)]
    public static extern int g_variant_is_of_type(IntPtr value, [MarshalAs(UnmanagedType.LPUTF8Str)] string type);

    [DllImport(LibGlib)]
    public static extern void g_variant_unref(IntPtr value);

    // --- Helpers ---

    public static IntPtr Tuple(params IntPtr[] children) => g_variant_new_tuple(children, (nuint)children.Length);

    public static IntPtr Array(IntPtr[] children) => g_variant_new_array(IntPtr.Zero, children, (nuint)children.Length);

    /// <summary>One <c>{sv}</c> entry of a vardict.</summary>
    public static IntPtr Entry(string key, IntPtr value) =>
        g_variant_new_dict_entry(g_variant_new_string(key), g_variant_new_variant(value));

    public static string ReadString(IntPtr utf8) => Marshal.PtrToStringUTF8(utf8) ?? "";

    /// <summary>Frees <paramref name="error"/> and returns its message.</summary>
    public static string TakeMessage(GError* error)
    {
        if (error == null) return "unknown error";
        var message = ReadString(error->Message);
        g_error_free(error);
        return message;
    }
}
