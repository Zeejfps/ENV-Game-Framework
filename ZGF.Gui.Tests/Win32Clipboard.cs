namespace ZGF.Gui.Tests;

using System;
using System.Runtime.InteropServices;
using System.Text;

public class Win32Clipboard : IClipboard
{
    const uint CF_UNICODETEXT = 13;
    const uint GMEM_MOVEABLE = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GlobalUnlock(IntPtr hMem);

    public void SetText(string text)
    {
        if (!OpenClipboard(IntPtr.Zero))
            throw new InvalidOperationException("Failed to open clipboard.");

        try
        {
            EmptyClipboard();

            // Convert string to bytes (UTF-16)
            byte[] bytes = Encoding.Unicode.GetBytes(text + "\0");
            UIntPtr size = (UIntPtr)bytes.Length;

            IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, size);
            if (hGlobal == IntPtr.Zero)
                throw new OutOfMemoryException("GlobalAlloc failed.");

            IntPtr target = GlobalLock(hGlobal);
            if (target == IntPtr.Zero)
                throw new InvalidOperationException("GlobalLock failed.");

            Marshal.Copy(bytes, 0, target, bytes.Length);
            GlobalUnlock(hGlobal);

            if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
                throw new InvalidOperationException("SetClipboardData failed.");
        }
        finally
        {
            CloseClipboard();
        }
    }

    public string? GetText()
    {
        if (!OpenClipboard(IntPtr.Zero))
            return null;

        try
        {
            IntPtr handle = GetClipboardData(CF_UNICODETEXT);
            if (handle == IntPtr.Zero)
                return null;

            IntPtr pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero)
                return null;

            string text = Marshal.PtrToStringUni(pointer) ?? "";
            GlobalUnlock(handle);

            return text;
        }
        finally
        {
            CloseClipboard();
        }
    }
}