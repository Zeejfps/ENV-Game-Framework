using ZGF.Desktop;

namespace ZGF.Gui.Desktop;

/// <summary>One driveable window in the live app, projected to what an inspector (the MCP server)
/// needs: its <see cref="Role"/>, the OS <see cref="Window"/> (for screen bounds, focus, redraw),
/// the mounted <see cref="Root"/> view, and the window's own <see cref="Input"/>. A view's
/// <c>Position</c> is in its window's canvas space — the same space <see cref="Input"/> dispatches
/// in — so a target found in one surface's root needs no translation, only injection into that
/// surface's input.</summary>
/// <param name="Scale">Device pixels per logical point for this window — what tells a reader whether
/// its screen bounds and its view positions are the same units.</param>
public sealed record GuiSurface(
    string Role,
    IWindow Window,
    View? Root,
    DesktopInputSystem Input,
    float Scale);
