using System.Runtime.InteropServices;
using System.Text;

namespace ZGF.Gui.Hosting;

internal sealed class Win32Clipboard : PlatformClipboard
{
    private const uint CfUnicodeText = 13;
    private const uint CfDib = 8;
    private const uint CfDibV5 = 17;
    private const uint GmemMoveable = 0x0002;

    // Another app (a clipboard history, a remote desktop client) can hold the clipboard open for
    // a moment, so opening it is retried briefly before giving up.
    private const int OpenAttempts = 10;
    private const int OpenRetryMilliseconds = 10;

    private readonly uint _htmlFormat = RegisterClipboardFormat(WindowsHtmlFormat.Name);

    public override void SetText(string text)
    {
        Open();
        try
        {
            EmptyClipboard();
            PutText(text);
        }
        finally
        {
            CloseClipboard();
        }
    }

    public override void SetHtml(string html, string text)
    {
        Open();
        try
        {
            EmptyClipboard();
            PutText(text);
            Put(_htmlFormat, WindowsHtmlFormat.Encode(html));
        }
        finally
        {
            CloseClipboard();
        }
    }

    public override string? GetText()
    {
        if (!TryOpen())
            return null;

        try
        {
            var handle = GetClipboardData(CfUnicodeText);
            if (handle == IntPtr.Zero)
                return null;

            var pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero)
                return null;

            var text = Marshal.PtrToStringUni(pointer) ?? "";
            GlobalUnlock(handle);
            return text;
        }
        finally
        {
            CloseClipboard();
        }
    }

    /// <summary>The bitmap on the clipboard, which Windows offers in both device-independent forms
    /// whichever was put there: the V5 one first, as only it can say a picture has alpha.</summary>
    public override ClipboardImage? GetImage()
    {
        if (!TryOpen())
            return null;

        try
        {
            if (Read(CfDibV5) is { } v5 && DeviceIndependentBitmap.Decode(v5) is { } fromV5)
                return fromV5;
            if (Read(CfDib) is { } dib)
                return DeviceIndependentBitmap.Decode(dib);
            return null;
        }
        finally
        {
            CloseClipboard();
        }
    }

    /// <summary>The bytes of the clipboard's <paramref name="format"/>, or null where it holds none;
    /// called while the clipboard is open.</summary>
    private static byte[]? Read(uint format)
    {
        var handle = GetClipboardData(format);
        if (handle == IntPtr.Zero)
            return null;

        var size = (ulong)GlobalSize(handle);
        if (size > (ulong)Array.MaxLength)
            return null;

        var pointer = GlobalLock(handle);
        if (pointer == IntPtr.Zero)
            return null;

        try
        {
            var bytes = new byte[(int)size];
            Marshal.Copy(pointer, bytes, 0, bytes.Length);
            return bytes;
        }
        finally
        {
            GlobalUnlock(handle);
        }
    }

    private static void Open()
    {
        if (!TryOpen())
            throw new InvalidOperationException("Failed to open clipboard.");
    }

    private static bool TryOpen()
    {
        for (var attempt = 1; attempt < OpenAttempts; attempt++)
        {
            if (OpenClipboard(IntPtr.Zero))
                return true;
            Thread.Sleep(OpenRetryMilliseconds);
        }
        return OpenClipboard(IntPtr.Zero);
    }

    private static void PutText(string text) => Put(CfUnicodeText, Encoding.Unicode.GetBytes(text + "\0"));

    private static void Put(uint format, byte[] bytes)
    {
        var handle = GlobalAlloc(GmemMoveable, (UIntPtr)bytes.Length);
        if (handle == IntPtr.Zero)
            throw new OutOfMemoryException("GlobalAlloc failed.");

        var target = GlobalLock(handle);
        if (target == IntPtr.Zero)
        {
            GlobalFree(handle);
            throw new InvalidOperationException("GlobalLock failed.");
        }

        Marshal.Copy(bytes, 0, target, bytes.Length);
        GlobalUnlock(handle);

        if (SetClipboardData(format, handle) != IntPtr.Zero)
            return;
        GlobalFree(handle);
        throw new InvalidOperationException("SetClipboardData failed.");
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint RegisterClipboardFormat(string format);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern UIntPtr GlobalSize(IntPtr hMem);
}
