using System.Runtime.InteropServices;

namespace ZGF.Gui.Hosting;

internal static unsafe class Libc
{
    private const string Library = "libc";

    public const short PollIn = 1;
    public const int EventFdCloseOnExec = 0x80000;
    public const int EventFdNonBlocking = 0x800;

    [StructLayout(LayoutKind.Sequential)]
    public struct PollFd
    {
        public int Fd;
        public short Events;
        public short ReturnedEvents;
    }

    [DllImport(Library)]
    public static extern int poll(PollFd* fds, nuint count, int timeoutMilliseconds);

    [DllImport(Library)]
    public static extern int eventfd(uint initialValue, int flags);

    [DllImport(Library)]
    public static extern nint read(int fd, void* buffer, nuint count);

    [DllImport(Library)]
    public static extern nint write(int fd, void* buffer, nuint count);

    [DllImport(Library)]
    public static extern int close(int fd);

    [DllImport(Library)]
    public static extern void free(void* pointer);
}
