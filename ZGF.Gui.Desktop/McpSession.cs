namespace ZGF.Gui.Desktop;

/// <summary>
/// One connected MCP client. <see cref="Ended"/> is cancelled when the client terminates the session
/// (Streamable HTTP <c>DELETE</c>) or the server stops, so a tool that waits on a person links its
/// wait to it and a vanished agent does not leave the UI hung. A client that simply disconnects
/// without terminating is not detected — nothing arrives on the wire — so a waiting tool still
/// needs its own bound.
/// </summary>
/// <remarks>Callbacks registered on <see cref="Ended"/> run on whichever thread ends the session,
/// never on the UI thread by contract; post to the UI thread before touching UI state.</remarks>
public sealed record McpSession(string Id, CancellationToken Ended);
