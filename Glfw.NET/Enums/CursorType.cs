namespace GLFW
{
    /// <summary>
    ///     Strongly-typed values describing possible cursor shapes.
    /// </summary>
    public enum CursorType
    {
        /// <summary>
        ///     The regular arrow cursor.
        /// </summary>
        Arrow = 0x00036001,

        /// <summary>
        ///     The text input I-beam cursor shape.
        /// </summary>
        Beam = 0x00036002,

        /// <summary>
        ///     The crosshair shape.
        /// </summary>
        Crosshair = 0x00036003,

        /// <summary>
        ///     The hand shape.
        /// </summary>
        Hand = 0x00036004,

        /// <summary>
        ///     The horizontal resize arrow shape.
        /// </summary>
        ResizeHorizontal = 0x00036005,

        /// <summary>
        ///     The vertical resize arrow shape.
        /// </summary>
        ResizeVertical = 0x00036006,

        /// <summary>
        ///     The top-left to bottom-right diagonal resize arrow shape. Requires GLFW 3.4 or newer;
        ///     older libraries reject it and return <see cref="Cursor.None" />.
        /// </summary>
        ResizeNwse = 0x00036007,

        /// <summary>
        ///     The top-right to bottom-left diagonal resize arrow shape. Requires GLFW 3.4 or newer;
        ///     older libraries reject it and return <see cref="Cursor.None" />.
        /// </summary>
        ResizeNesw = 0x00036008
    }
}