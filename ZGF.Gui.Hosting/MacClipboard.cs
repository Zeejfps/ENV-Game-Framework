using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static ZGF.Rendering.Metal.Objc;

namespace ZGF.Gui.Hosting;

// The general NSPasteboard, through the Objective-C runtime. Every call runs in its own
// autorelease pool, since a copy can come from outside the run loop's pool.
[SupportedOSPlatform("macos")]
internal sealed class MacClipboard : PlatformClipboard
{
    private const string TextType = "public.utf8-plain-text";
    private const string HtmlType = "public.html";

    // What a screenshot or a copied picture is put on the pasteboard as, in the order to prefer.
    private static readonly string[] PictureTypes = ["public.png", "public.tiff"];

    public override void SetText(string text)
    {
        var pool = objc_autoreleasePoolPush();
        try
        {
            var pasteboard = ClearPasteboard();
            Put(pasteboard, TextType, text);
        }
        finally
        {
            objc_autoreleasePoolPop(pool);
        }
    }

    public override void SetHtml(string html, string text)
    {
        var pool = objc_autoreleasePoolPush();
        try
        {
            var pasteboard = ClearPasteboard();
            Put(pasteboard, TextType, text);
            Put(pasteboard, HtmlType, HtmlDocument.WithCharset(html));
        }
        finally
        {
            objc_autoreleasePoolPop(pool);
        }
    }

    public override string? GetText()
    {
        var pool = objc_autoreleasePoolPush();
        try
        {
            var pasteboard = GeneralPasteboard();
            var text = msg_IntPtr(pasteboard, Sel("stringForType:"), NSString(TextType));
            if (text == IntPtr.Zero)
                return null;
            return Marshal.PtrToStringUTF8(msg_IntPtr(text, Sel("UTF8String")));
        }
        finally
        {
            objc_autoreleasePoolPop(pool);
        }
    }

    public override ClipboardImage? GetImage()
    {
        var pool = objc_autoreleasePoolPush();
        try
        {
            var pasteboard = GeneralPasteboard();
            foreach (var type in PictureTypes)
            {
                var data = msg_IntPtr(pasteboard, Sel("dataForType:"), NSString(type));
                if (data != IntPtr.Zero && MacPicture.Decode(data) is { } picture)
                    return picture;
            }
            return null;
        }
        finally
        {
            objc_autoreleasePoolPop(pool);
        }
    }

    private static IntPtr ClearPasteboard()
    {
        var pasteboard = GeneralPasteboard();
        msg_IntPtr(pasteboard, Sel("clearContents"));
        return pasteboard;
    }

    private static IntPtr GeneralPasteboard() => msg_IntPtr(Class("NSPasteboard"), Sel("generalPasteboard"));

    private static void Put(IntPtr pasteboard, string type, string value) =>
        msg_IntPtr(pasteboard, Sel("setString:forType:"), NSString(value), NSString(type));

    // An autoreleased NSString, via +[NSString stringWithUTF8String:].
    private static IntPtr NSString(string value)
    {
        var utf8 = Marshal.StringToCoTaskMemUTF8(value);
        try
        {
            return msg_IntPtr(Class("NSString"), Sel("stringWithUTF8String:"), utf8);
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8);
        }
    }
}
