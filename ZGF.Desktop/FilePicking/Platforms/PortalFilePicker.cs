using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static ZGF.Desktop.FilePicking.Gio;

namespace ZGF.Desktop.FilePicking;

/// <summary>What became of one portal dialog.</summary>
internal abstract record PortalOutcome
{
    private PortalOutcome() { }

    public sealed record Picked(string Path) : PortalOutcome;

    /// <summary>The user dismissed the dialog, or it closed without a usable file.</summary>
    public sealed record Cancelled : PortalOutcome;

    /// <summary>No portal could show this dialog; another picker should be tried.</summary>
    public sealed record Unavailable(string Reason) : PortalOutcome;
}

/// <summary>
/// Shows file dialogs through the XDG desktop portal's <c>org.freedesktop.portal.FileChooser</c>,
/// which every current Linux desktop provides (GNOME, KDE, XFCE and Cinnamon through
/// <c>xdg-desktop-portal-gtk</c>, and so on) whatever toolkit it is built with, on X11 and Wayland
/// alike, and from inside a Flatpak. The dialog belongs to the desktop, so it matches the user's
/// file manager.
/// </summary>
/// <remarks>
/// A portal dialog is asynchronous: the method call returns a request object at once, and the
/// choice arrives later as that object's <c>Response</c> signal. <see cref="Pick"/> blocks its
/// calling thread until then and must not be called on the UI thread.
/// </remarks>
[SupportedOSPlatform("linux")]
internal static unsafe class PortalFilePicker
{
    private const string PortalBusName = "org.freedesktop.portal.Desktop";
    private const string PortalObjectPath = "/org/freedesktop/portal/desktop";
    private const string FileChooserInterface = "org.freedesktop.portal.FileChooser";

    /// <summary>The FileChooser version that added <c>directory</c> to <c>OpenFile</c>.</summary>
    private const uint FolderModeVersion = 3;

    /// <summary>The <c>Response</c> code of a dialog that ended with a choice.</summary>
    private const uint ResponseSuccess = 0;

    /// <summary>Collects the outcome the signal callback parses, on the thread that waits for it.</summary>
    private sealed class Waiter
    {
        public PortalOutcome? Result;
    }

    public static PortalOutcome Pick(
        LinuxChooser chooser,
        string title,
        string? initialDirectory,
        string? suggestedFileName,
        IReadOnlyList<FileFilter>? filters)
    {
        try
        {
            return PickOnBus(chooser, title, initialDirectory, suggestedFileName, filters);
        }
        catch (DllNotFoundException e)
        {
            return new PortalOutcome.Unavailable($"GIO is not installed ({e.Message})");
        }
        catch (EntryPointNotFoundException e)
        {
            return new PortalOutcome.Unavailable($"GIO is too old ({e.Message})");
        }
    }

    private static PortalOutcome PickOnBus(
        LinuxChooser chooser,
        string title,
        string? initialDirectory,
        string? suggestedFileName,
        IReadOnlyList<FileFilter>? filters)
    {
        var connection = g_bus_get_sync(G_BUS_TYPE_SESSION, IntPtr.Zero, out var busError);
        if (connection == IntPtr.Zero)
            return new PortalOutcome.Unavailable($"no session bus: {TakeMessage(busError)}");

        try
        {
            var version = QueryFileChooserVersion(connection);
            if (version == null)
                return new PortalOutcome.Unavailable("no desktop portal provides a FileChooser");
            if (chooser == LinuxChooser.Folder && version < FolderModeVersion)
                return new PortalOutcome.Unavailable($"the FileChooser portal (v{version}) cannot pick folders");

            return ShowDialog(connection, chooser, title, initialDirectory, suggestedFileName, filters);
        }
        finally
        {
            g_object_unref(connection);
        }
    }

    private static PortalOutcome ShowDialog(
        IntPtr connection,
        LinuxChooser chooser,
        string title,
        string? initialDirectory,
        string? suggestedFileName,
        IReadOnlyList<FileFilter>? filters)
    {
        // The portal names the request object after the caller's unique bus name and the token the
        // caller picks, so the Response can be subscribed to before the call, leaving no window in
        // which a fast reply could be missed.
        var token = "zgf" + Guid.NewGuid().ToString("N");
        var sender = ReadString(g_dbus_connection_get_unique_name(connection)).TrimStart(':').Replace('.', '_');
        var requestPath = $"{PortalObjectPath}/request/{sender}/{token}";

        // Signals are dispatched to the context that was this thread's default when subscribing,
        // and only while this thread iterates it.
        var context = g_main_context_new();
        g_main_context_push_thread_default(context);

        var waiter = new Waiter();
        var waiterHandle = GCHandle.Alloc(waiter);
        var userData = GCHandle.ToIntPtr(waiterHandle);
        List<uint> subscriptions = [];
        try
        {
            subscriptions.Add(SubscribeToResponse(connection, requestPath, userData));
            // A portal that exits while its dialog is up never answers; without this the wait
            // would never end.
            subscriptions.Add(g_dbus_connection_signal_subscribe(
                connection,
                "org.freedesktop.DBus",
                "org.freedesktop.DBus",
                "NameOwnerChanged",
                "/org/freedesktop/DBus",
                PortalBusName,
                G_DBUS_SIGNAL_FLAGS_NONE,
                &OnSignal,
                userData,
                IntPtr.Zero));

            var method = chooser == LinuxChooser.SaveName ? "SaveFile" : "OpenFile";
            var parameters = BuildParameters(chooser, title, token, initialDirectory, suggestedFileName, filters);
            var reply = g_dbus_connection_call_sync(
                connection,
                PortalBusName,
                PortalObjectPath,
                FileChooserInterface,
                method,
                parameters,
                "(o)",
                G_DBUS_CALL_FLAGS_NONE,
                DefaultTimeout,
                IntPtr.Zero,
                out var callError);
            if (reply == IntPtr.Zero)
                return new PortalOutcome.Unavailable($"{method} failed: {TakeMessage(callError)}");

            var handlePath = ReadFirstString(reply);
            g_variant_unref(reply);

            // Portals before 0.9 ignore handle_token and choose the path themselves.
            if (handlePath != requestPath)
                subscriptions.Add(SubscribeToResponse(connection, handlePath, userData));

            while (waiter.Result == null)
                g_main_context_iteration(context, 1);

            return waiter.Result;
        }
        finally
        {
            foreach (var subscription in subscriptions)
                g_dbus_connection_signal_unsubscribe(connection, subscription);

            // Run any callback already queued so none is left holding the handle freed below.
            while (g_main_context_iteration(context, 0) != 0) { }

            g_main_context_pop_thread_default(context);
            g_main_context_unref(context);
            waiterHandle.Free();
        }
    }

    private static uint SubscribeToResponse(IntPtr connection, string requestPath, IntPtr userData) =>
        g_dbus_connection_signal_subscribe(
            connection,
            PortalBusName,
            "org.freedesktop.portal.Request",
            "Response",
            requestPath,
            null,
            G_DBUS_SIGNAL_FLAGS_NONE,
            &OnSignal,
            userData,
            IntPtr.Zero);

    /// <summary>The FileChooser interface's version, or null when no portal provides one.</summary>
    private static uint? QueryFileChooserVersion(IntPtr connection)
    {
        var reply = g_dbus_connection_call_sync(
            connection,
            PortalBusName,
            PortalObjectPath,
            "org.freedesktop.DBus.Properties",
            "Get",
            Tuple(g_variant_new_string(FileChooserInterface), g_variant_new_string("version")),
            "(v)",
            G_DBUS_CALL_FLAGS_NONE,
            DefaultTimeout,
            IntPtr.Zero,
            out var error);
        if (reply == IntPtr.Zero)
        {
            Console.WriteLine($"[FilePicker] Desktop portal FileChooser not found: {TakeMessage(error)}");
            return null;
        }

        var boxed = g_variant_get_child_value(reply, 0);
        var value = g_variant_get_variant(boxed);
        uint? version = g_variant_is_of_type(value, "u") != 0 ? g_variant_get_uint32(value) : null;
        g_variant_unref(value);
        g_variant_unref(boxed);
        g_variant_unref(reply);
        return version;
    }

    /// <summary>The <c>(ssa{sv})</c> arguments of <c>OpenFile</c>/<c>SaveFile</c>.</summary>
    private static IntPtr BuildParameters(
        LinuxChooser chooser,
        string title,
        string token,
        string? initialDirectory,
        string? suggestedFileName,
        IReadOnlyList<FileFilter>? filters)
    {
        List<IntPtr> options =
        [
            Entry("handle_token", g_variant_new_string(token)),
            Entry("modal", g_variant_new_boolean(1)),
        ];

        if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
            options.Add(Entry("current_folder", g_variant_new_bytestring(initialDirectory)));

        switch (chooser)
        {
            case LinuxChooser.Folder:
                options.Add(Entry("directory", g_variant_new_boolean(1)));
                break;
            case LinuxChooser.SaveName:
                if (!string.IsNullOrEmpty(suggestedFileName))
                    options.Add(Entry("current_name", g_variant_new_string(suggestedFileName)));
                break;
            case LinuxChooser.File:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(chooser), chooser, null);
        }

        if (chooser != LinuxChooser.Folder && filters != null)
        {
            var filterList = BuildFilters(filters);
            if (filterList != IntPtr.Zero)
                options.Add(Entry("filters", filterList));
        }

        // The parent window is left empty: the portal then opens the dialog unparented, on top.
        return Tuple(g_variant_new_string(""), g_variant_new_string(title), Array([.. options]));
    }

    /// <summary>
    /// The <c>a(sa(us))</c> filter list: each filter's name and its patterns as globs (kind 0), or
    /// zero when no filter has a pattern, as an array must not be empty here.
    /// </summary>
    private static IntPtr BuildFilters(IReadOnlyList<FileFilter> filters)
    {
        List<IntPtr> groups = [];
        foreach (var filter in filters)
        {
            if (filter.Patterns.Count == 0) continue;

            var patterns = new IntPtr[filter.Patterns.Count];
            for (var i = 0; i < patterns.Length; i++)
                patterns[i] = Tuple(g_variant_new_uint32(0), g_variant_new_string(filter.Patterns[i]));
            groups.Add(Tuple(g_variant_new_string(filter.Name), Array(patterns)));
        }
        return groups.Count == 0 ? IntPtr.Zero : Array([.. groups]);
    }

    private static string ReadFirstString(IntPtr tuple)
    {
        var child = g_variant_get_child_value(tuple, 0);
        var value = ReadString(g_variant_get_string(child, IntPtr.Zero));
        g_variant_unref(child);
        return value;
    }

    /// <summary>
    /// Handles both subscribed signals. Runs on the waiting thread, inside
    /// <c>g_main_context_iteration</c>; an exception here would cross into native code and end the
    /// process, so everything it calls is total.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnSignal(
        IntPtr connection,
        IntPtr senderName,
        IntPtr objectPath,
        IntPtr interfaceName,
        IntPtr signalName,
        IntPtr parameters,
        IntPtr userData)
    {
        if (GCHandle.FromIntPtr(userData).Target is not Waiter waiter) return;
        if (waiter.Result != null) return;

        var signal = ReadString(signalName);
        if (signal == "Response")
        {
            waiter.Result = ParseResponse(parameters);
        }
        else if (signal == "NameOwnerChanged" && PortalLeftBus(parameters))
        {
            Console.WriteLine("[FilePicker] The desktop portal exited while its dialog was open.");
            waiter.Result = new PortalOutcome.Cancelled();
        }
    }

    /// <summary>Whether a <c>NameOwnerChanged</c> <c>(sss)</c> says the name now has no owner.</summary>
    private static bool PortalLeftBus(IntPtr parameters)
    {
        if (g_variant_is_of_type(parameters, "(sss)") == 0) return false;

        var newOwner = g_variant_get_child_value(parameters, 2);
        var gone = ReadString(g_variant_get_string(newOwner, IntPtr.Zero)).Length == 0;
        g_variant_unref(newOwner);
        return gone;
    }

    /// <summary>
    /// Parses a <c>Response</c>'s <c>(ua{sv})</c>: a code, then results whose <c>uris</c> holds the
    /// chosen file as a <c>file://</c> URI.
    /// </summary>
    private static PortalOutcome ParseResponse(IntPtr parameters)
    {
        if (g_variant_is_of_type(parameters, "(ua{sv})") == 0)
        {
            Console.WriteLine("[FilePicker] The desktop portal sent a malformed response.");
            return new PortalOutcome.Cancelled();
        }

        var codeValue = g_variant_get_child_value(parameters, 0);
        var code = g_variant_get_uint32(codeValue);
        g_variant_unref(codeValue);
        // 1 is a cancel; 2 is "ended some other way", which GTK's backend also reports for Escape.
        if (code != ResponseSuccess) return new PortalOutcome.Cancelled();

        var results = g_variant_get_child_value(parameters, 1);
        var uris = g_variant_lookup_value(results, "uris", "as");
        g_variant_unref(results);
        if (uris == IntPtr.Zero) return new PortalOutcome.Cancelled();

        string? uri = null;
        if (g_variant_n_children(uris) > 0)
        {
            var first = g_variant_get_child_value(uris, 0);
            uri = ReadString(g_variant_get_string(first, IntPtr.Zero));
            g_variant_unref(first);
        }
        g_variant_unref(uris);

        if (uri == null) return new PortalOutcome.Cancelled();
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) || !parsed.IsFile)
        {
            Console.WriteLine($"[FilePicker] The desktop portal returned a non-file URI: {uri}");
            return new PortalOutcome.Cancelled();
        }
        // LocalPath undoes the URI's percent-encoding.
        return new PortalOutcome.Picked(parsed.LocalPath);
    }
}
