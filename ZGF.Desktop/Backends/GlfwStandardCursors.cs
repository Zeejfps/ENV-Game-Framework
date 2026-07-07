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

        var created = GLFW.Glfw.CreateStandardCursor(ToCursorType(cursor));
        Cache[cursor] = created;
        return created;
    }

    private static CursorType ToCursorType(MouseCursor cursor) => cursor switch
    {
        MouseCursor.Text => CursorType.Beam,
        MouseCursor.Hand => CursorType.Hand,
        MouseCursor.Crosshair => CursorType.Crosshair,
        MouseCursor.ResizeHorizontal => CursorType.ResizeHorizontal,
        MouseCursor.ResizeVertical => CursorType.ResizeVertical,
        _ => CursorType.Arrow,
    };
}
