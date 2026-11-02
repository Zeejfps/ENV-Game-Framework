using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ZGF.WebView.Cef;

/// <summary>
/// Chromium asks <c>NSApp</c> whether it is inside <c>sendEvent:</c> (<c>CrAppProtocol</c>) and tells it
/// when it is (<c>CrAppControlProtocol</c>). The window host's <c>NSApplication</c> answers neither, so
/// the two methods and protocols are added to its class before CEF starts.
/// </summary>
[SupportedOSPlatform("macos")]
internal static unsafe class MacApplication
{
    private const string ObjcLib = "/usr/lib/libobjc.A.dylib";

    // There is one NSApp per process, so whether it is handling an event is process-wide too.
    private static bool _handlingSendEvent;

    public static void AdoptCefAppProtocol()
    {
        var app = objc_msgSend(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
        var appClass = object_getClass(app);
        if (class_respondsToSelector(appClass, sel_registerName("isHandlingSendEvent")))
            return;

        // BOOL is C bool on arm64 and signed char on x86_64.
        var boolType = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "B" : "c";
        class_addMethod(appClass, sel_registerName("isHandlingSendEvent"),
            (nint)(delegate* unmanaged<nint, nint, byte>)&IsHandlingSendEvent, boolType + "@:");
        class_addMethod(appClass, sel_registerName("setHandlingSendEvent:"),
            (nint)(delegate* unmanaged<nint, nint, byte, void>)&SetHandlingSendEvent, "v@:" + boolType);

        foreach (var name in new[] { "CrAppProtocol", "CrAppControlProtocol", "CefAppProtocol" })
        {
            var protocol = objc_getProtocol(name);
            if (protocol != 0)
                class_addProtocol(appClass, protocol);
        }
    }

    [UnmanagedCallersOnly]
    private static byte IsHandlingSendEvent(nint self, nint cmd) => _handlingSendEvent ? (byte)1 : (byte)0;

    [UnmanagedCallersOnly]
    private static void SetHandlingSendEvent(nint self, nint cmd, byte handling) => _handlingSendEvent = handling != 0;

    [DllImport(ObjcLib)]
    private static extern nint objc_getClass(string name);

    [DllImport(ObjcLib)]
    private static extern nint objc_getProtocol(string name);

    [DllImport(ObjcLib)]
    private static extern nint sel_registerName(string name);

    [DllImport(ObjcLib)]
    private static extern nint object_getClass(nint obj);

    [DllImport(ObjcLib)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool class_respondsToSelector(nint cls, nint sel);

    [DllImport(ObjcLib)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool class_addMethod(nint cls, nint name, nint imp, string types);

    [DllImport(ObjcLib)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool class_addProtocol(nint cls, nint protocol);

    [DllImport(ObjcLib)]
    private static extern nint objc_msgSend(nint receiver, nint selector);
}
