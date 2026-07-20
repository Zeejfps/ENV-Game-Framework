using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace GLFW
{
    /// <summary>
    ///     Function signature for receiving IME composition (preedit) updates.
    ///     <para>
    ///         The composition string arrives as <paramref name="preeditCount" /> UTF-32 code points at
    ///         <paramref name="preeditString" />, divided into <paramref name="blockCount" /> clauses whose lengths
    ///         (in code points) are the <see cref="int" />s at <paramref name="blockSizes" />. Both pointers belong to
    ///         GLFW and are only valid for the duration of the call.
    ///     </para>
    ///     <para>
    ///         A callback with <paramref name="preeditCount" /> of 0 means the composition ended. It cannot
    ///         distinguish commit from cancel — on commit the text arrives separately through the char callback.
    ///     </para>
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PreeditCallback(Window window, int preeditCount, IntPtr preeditString, int blockCount,
        IntPtr blockSizes, int focusedBlock, int caret);

    /// <summary>
    ///     The IME/preedit half of the GLFW input API. This is not part of stock GLFW: it comes from the
    ///     IM-support patch (glfw/glfw#2130) carried by the natives in <c>Native/</c>. Because an unpatched GLFW
    ///     would make every one of these entry points a hard <see cref="EntryPointNotFoundException" />, call
    ///     <see cref="IsSupported" /> before using any of them.
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    public static class GlfwIme
    {
        private static bool? _isSupported;
        private static bool? _isTextInputFocusSupported;

        /// <summary>
        ///     True when the loaded GLFW carries the IM-support patch. When false, every other member of this class
        ///     will throw, and the app has no IME: CJK cannot be typed, but nothing else is affected.
        /// </summary>
        public static bool IsSupported => _isSupported ??= ProbeExport("glfwSetPreeditCallback");

        /// <summary>
        ///     True when the loaded GLFW can scope the IME to a focused text field. Probed separately from
        ///     <see cref="IsSupported" /> on purpose: this is a strictly newer capability, and a native carrying the
        ///     preedit entry points but not this one would otherwise turn <see cref="SetTextInputFocus" /> into an
        ///     <see cref="EntryPointNotFoundException" /> instead of degrading to the legacy always-on behaviour.
        /// </summary>
        public static bool IsTextInputFocusSupported =>
            _isTextInputFocusSupported ??= ProbeExport("glfwSetTextInputFocus");

        [DllImport(Glfw.LIBRARY, EntryPoint = "glfwSetPreeditCallback", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.FunctionPtr, MarshalTypeRef = typeof(PreeditCallback))]
        public static extern PreeditCallback SetPreeditCallback(Window window, PreeditCallback callback);

        /// <summary>
        ///     Tells the IME where the caret is, in window coordinates with the origin at the top-left, so the OS
        ///     candidate window can position itself against it. Without this it sits at the window origin.
        /// </summary>
        [DllImport(Glfw.LIBRARY, EntryPoint = "glfwSetPreeditCursorRectangle", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetPreeditCursorRectangle(Window window, int x, int y, int width, int height);

        /// <summary>
        ///     Abandons any in-flight composition. The IME will not deliver the text, so this discards it rather
        ///     than committing it — call it when the field loses focus, not when the user accepts.
        /// </summary>
        [DllImport(Glfw.LIBRARY, EntryPoint = "glfwResetPreeditText", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ResetPreeditText(Window window);

        /// <summary>
        ///     Declares whether the window is currently editing text. With focus off the IME stops consuming
        ///     keystrokes, so bare-letter shortcuts reach the app while a CJK input method is active; with it on,
        ///     composition works as normal.
        ///     <para>
        ///         GLFW treats the window as always-focused until this is called once, so a window that never calls
        ///         it keeps the legacy behaviour. Arm each window at creation rather than waiting for the first blur.
        ///     </para>
        ///     <para>
        ///         This is the IME's <em>routing</em>, not its conversion mode — it does not touch the user's
        ///         Chinese-vs-alphanumeric setting, which is theirs and not ours to change.
        ///     </para>
        /// </summary>
        [DllImport(Glfw.LIBRARY, EntryPoint = "glfwSetTextInputFocus", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetTextInputFocus(Window window, bool focused);

        /// <summary>
        ///     Looks the symbol up in the loaded native rather than calling it, so an unpatched GLFW reports
        ///     "unsupported" instead of throwing.
        /// </summary>
        private static bool ProbeExport(string symbol)
        {
            var assembly = typeof(Glfw).Assembly;
            foreach (var candidate in LibraryNames())
            {
                // Keep going when a candidate loads but lacks the symbol: on Linux the first name
                // that resolves may be the distro's unpatched GLFW, and stopping there would report
                // "unsupported" while the P/Invoke went on to bind the app-local patched one.
                if (NativeLibrary.TryLoad(candidate, assembly, DllImportSearchPath.AssemblyDirectory, out var handle)
                    && NativeLibrary.TryGetExport(handle, symbol, out _))
                    return true;
            }

            return false;
        }

        // Mirrors NativeLibraryResolver's probing order: on Linux the app-local native is named by its soname,
        // which the default probing for "glfw3" never tries.
        private static string[] LibraryNames() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? new[] { "libglfw.so.3", "libglfw.so", Glfw.LIBRARY }
                : new[] { Glfw.LIBRARY };
    }
}
