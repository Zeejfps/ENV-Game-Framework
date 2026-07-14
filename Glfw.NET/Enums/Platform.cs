namespace GLFW
{
    /// <summary>
    ///     The windowing platform GLFW binds to, selected with <see cref="Hint.Platform" /> before
    ///     <see cref="Glfw.Init" />.
    /// </summary>
    public enum Platform
    {
        /// <summary>
        ///     Let GLFW pick the first platform it can connect to. On Linux this prefers
        ///     <see cref="Wayland" /> over <see cref="X11" /> whenever a Wayland session is present.
        /// </summary>
        Any = 0x00060000,

        Win32 = 0x00060001,

        Cocoa = 0x00060002,

        Wayland = 0x00060003,

        X11 = 0x00060004,

        /// <summary>
        ///     The headless platform, which creates no windows and provides no input.
        /// </summary>
        Null = 0x00060005
    }
}
