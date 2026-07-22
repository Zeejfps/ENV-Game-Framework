namespace WebPSharp.Api;

/// <summary>
/// How an animation frame is combined with the canvas.
/// </summary>
public enum WebPBlendMethod
{
    /// <summary>Alpha-blend the frame over the current canvas contents.</summary>
    Over = 0,

    /// <summary>Overwrite the frame rectangle, ignoring the existing canvas.</summary>
    Source = 1,
}

/// <summary>
/// What happens to the frame rectangle after a frame is displayed.
/// </summary>
public enum WebPDisposalMethod
{
    /// <summary>Leave the canvas unchanged for the next frame.</summary>
    None = 0,

    /// <summary>Fill the frame rectangle with the background color before the next frame.</summary>
    Background = 1,
}
