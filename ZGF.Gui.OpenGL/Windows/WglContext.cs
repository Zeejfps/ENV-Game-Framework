using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ZGF.Gui.OpenGL.Windows;

/// <summary>A WGL context and the device context it was made current with.</summary>
[SupportedOSPlatform("windows")]
internal readonly partial record struct WglContext(nint DeviceContext, nint RenderingContext)
{
    /// <summary>The context current on this thread; both handles are zero when there is none.</summary>
    public static WglContext Current => new(GetCurrentDC(), GetCurrentContext());

    public bool IsNone => RenderingContext == 0;

    /// <summary>
    /// Makes <paramref name="target"/> current until the scope is disposed, which makes the caller's context current
    /// again; the caller's exact context comes back, so GLFW's record of what is current stays true. When
    /// <paramref name="target"/> cannot be made current, the caller's context is made current again, the failure is
    /// reported, and <see cref="Scope.Entered"/> is false: do not go on with the work. Bringing the caller's context
    /// back failing throws, since the thread is then left with no context the rest of the app expects.
    /// </summary>
    public static Scope Enter(WglContext target, string doing)
    {
        var caller = Current;
        if (caller == target)
            return new Scope(caller, doing, entered: true, switched: false);
        if (MakeCurrent(target.DeviceContext, target.RenderingContext))
            return new Scope(caller, doing, entered: true, switched: true);
        var error = Marshal.GetLastPInvokeError();
        Restore(caller, doing);
        Console.Error.WriteLine($"[WGL] {doing}: wglMakeCurrent failed (error {error}); not done");
        return new Scope(caller, doing, entered: false, switched: false);
    }

    private static void Restore(WglContext caller, string doing)
    {
        if (!MakeCurrent(caller.DeviceContext, caller.RenderingContext))
            throw new InvalidOperationException($"{doing}: wglMakeCurrent failed (error {Marshal.GetLastPInvokeError()}) making the caller's context current again.");
    }

    public readonly struct Scope : IDisposable
    {
        private readonly WglContext _caller;
        private readonly string _doing;
        private readonly bool _switched;

        internal Scope(WglContext caller, string doing, bool entered, bool switched)
        {
            _caller = caller;
            _doing = doing;
            Entered = entered;
            _switched = switched;
        }

        /// <summary>Whether the target context is current, so the work may go on.</summary>
        public bool Entered { get; }

        public void Dispose()
        {
            if (_switched)
                Restore(_caller, _doing);
        }
    }

    [LibraryImport("opengl32.dll", EntryPoint = "wglGetCurrentContext")]
    private static partial nint GetCurrentContext();

    [LibraryImport("opengl32.dll", EntryPoint = "wglGetCurrentDC")]
    private static partial nint GetCurrentDC();

    [LibraryImport("opengl32.dll", EntryPoint = "wglMakeCurrent", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool MakeCurrent(nint deviceContext, nint renderingContext);
}
