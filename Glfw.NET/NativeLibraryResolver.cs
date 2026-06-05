using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GLFW
{
    /// <summary>
    ///     Maps the <see cref="Glfw.LIBRARY" /> import name to the platform's actual GLFW filename.
    ///     <para>
    ///         Windows and macOS ship a native renamed to match the import (<c>glfw3.dll</c> / <c>glfw3</c>),
    ///         so the default loader finds them. Linux distros install GLFW as <c>libglfw.so.3</c>, which the
    ///         default probing for <c>"glfw3"</c> never tries — without this resolver the import fails with
    ///         "unable to load shared library glfw3". Install the distro package (e.g. <c>apt install libglfw3</c>).
    ///     </para>
    /// </summary>
    internal static class NativeLibraryResolver
    {
        // Must run before Glfw's static ctor triggers the first P/Invoke, so a module
        // initializer (not a lazy hook) is the right tool despite CA2255.
#pragma warning disable CA2255
        [ModuleInitializer]
        internal static void Register()
        {
            NativeLibrary.SetDllImportResolver(typeof(Glfw).Assembly, Resolve);
        }
#pragma warning restore CA2255

        private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName != Glfw.LIBRARY)
                return IntPtr.Zero;

            foreach (var candidate in Candidates())
            {
                if (NativeLibrary.TryLoad(candidate, assembly, searchPath, out var handle))
                    return handle;
            }

            // Fall back to the default loader so the runtime throws its usual diagnostic.
            return IntPtr.Zero;
        }

        private static IEnumerable<string> Candidates()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                yield return "libglfw.so.3";
                yield return "libglfw.so";
            }
        }
    }
}
