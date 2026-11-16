using System.Runtime.InteropServices;
using GLFW;

namespace ZGF.Desktop.Backends;

// Lazily creates and caches GLFW cursor objects: the platform's standard shapes, and the shapes ZGF draws itself
// (DrawnCursors) at each scale a window asks for. These are process-global GLFW handles safe to share across windows;
// created on demand from the main thread, where the render/input loop already drives GLFW. They live as long as GLFW
// does: Release runs before it terminates.
internal static class GlfwCursors
{
    private static readonly Dictionary<(MouseCursor Cursor, int Scale), Cursor> Cache = new();

    public static void Release()
    {
        foreach (var cursor in Cache.Values)
        {
            if (cursor != Cursor.None)
                GLFW.Glfw.DestroyCursor(cursor);
        }
        Cache.Clear();
    }

    /// <summary>The GLFW cursor for <paramref name="cursor"/> in a window at <paramref name="contentScale"/>, which
    /// sizes the shapes ZGF draws; the platform sizes its own.</summary>
    public static Cursor Get(MouseCursor cursor, float contentScale)
    {
        // NULL resets the window to the system arrow — no object to create.
        if (cursor == MouseCursor.Default)
            return Cursor.None;

        var key = (cursor, DrawnCursors.Draws(cursor) ? DrawnScale(contentScale) : 1);
        if (Cache.TryGetValue(key, out var existing))
            return existing;

        var created = DrawnCursors.Draws(cursor) ? CreateDrawn(cursor, key.Item2) : CreateStandard(cursor);
        Cache[key] = created;
        return created;
    }

    // Windows and X11 show a cursor image pixel for pixel, so a drawn hand is scaled up with the screen as the system's
    // own cursors are. macOS sizes it in points and scales it itself.
    private static int DrawnScale(float contentScale) =>
        OperatingSystem.IsMacOS() ? 1 : Math.Max(1, (int)MathF.Round(contentScale));

    private static Cursor CreateDrawn(MouseCursor cursor, int scale)
    {
        var drawn = DrawnCursors.Draw(cursor, scale);
        var pixels = GCHandle.Alloc(drawn.Rgba, GCHandleType.Pinned);
        try
        {
            var image = new Image(drawn.Size, drawn.Size, pixels.AddrOfPinnedObject());
            return GLFW.Glfw.CreateCursor(ref image, drawn.HotspotX, drawn.HotspotY);
        }
        finally
        {
            pixels.Free();
        }
    }

    // The diagonal and move shapes only exist from GLFW 3.4 on. An older native library doesn't recognize the
    // constant and hands back a NULL handle, which SetCursor reads as "system arrow" — so the pointer would sit on
    // an arrow over a resize grip with nothing to explain it. Fall back to the nearest shape that still says what
    // the pointer does. The caller caches whatever comes back, so the failed probe happens once per shape rather
    // than on every frame's cursor push.
    private static Cursor CreateStandard(MouseCursor cursor)
    {
        var created = GLFW.Glfw.CreateStandardCursor(ToCursorType(cursor));
        if (created != Cursor.None)
            return created;

        if (cursor is MouseCursor.ResizeNwse or MouseCursor.ResizeNesw)
            return GLFW.Glfw.CreateStandardCursor(CursorType.ResizeHorizontal);
        if (cursor == MouseCursor.Move)
            return GLFW.Glfw.CreateStandardCursor(CursorType.Crosshair);

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
        MouseCursor.Move => CursorType.ResizeAll,
        _ => CursorType.Arrow,
    };
}
