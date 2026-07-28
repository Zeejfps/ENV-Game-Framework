using GLFW;

namespace ZGF.Desktop.Backends;

// Lazily creates and caches the GLFW standard cursor objects. These are process-global GLFW
// handles safe to share across windows; created on demand from the main thread, where the
// render/input loop already drives GLFW.
internal static class GlfwStandardCursors
{
    private static readonly Dictionary<MouseCursor, Cursor> Cache = new();

    public static Cursor Get(MouseCursor cursor)
    {
        // NULL resets the window to the system arrow — no object to create.
        if (cursor == MouseCursor.Default)
            return Cursor.None;

        if (Cache.TryGetValue(cursor, out var existing))
            return existing;

        var created = Create(cursor);
        Cache[cursor] = created;
        return created;
    }

    // The diagonal shapes only exist from GLFW 3.4 on. An older native library doesn't recognize the
    // constant and hands back a NULL handle, which SetCursor reads as "system arrow" — so the pointer
    // would sit on an arrow over a resize grip with nothing to explain it. Fall back to the nearest
    // axis shape, which at least still says "this resizes". The caller caches whatever comes back, so
    // the failed probe happens once per shape rather than on every frame's cursor push.
    private static Cursor Create(MouseCursor cursor)
    {
        var created = GLFW.Glfw.CreateStandardCursor(ToCursorType(cursor));
        if (created != Cursor.None)
            return created;

        if (cursor is MouseCursor.ResizeNwse or MouseCursor.ResizeNesw)
            return GLFW.Glfw.CreateStandardCursor(CursorType.ResizeHorizontal);

        return created;
    }

    private static CursorType ToCursorType(MouseCursor cursor) => cursor switch
    {
        MouseCursor.Text => CursorType.Beam,
        MouseCursor.Hand => CursorType.Hand,
        MouseCursor.Crosshair => CursorType.Crosshair,
        MouseCursor.ResizeHorizontal => CursorType.ResizeHorizontal,
        MouseCursor.ResizeVertical => CursorType.ResizeVertical,
        MouseCursor.ResizeNwse => CursorType.ResizeNwse,
        MouseCursor.ResizeNesw => CursorType.ResizeNesw,
        _ => CursorType.Arrow,
    };
}
