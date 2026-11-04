namespace ZGF.Gui;

public enum SharedTextureKind
{
    /// <summary>An NT handle to a D3D11 texture, opened with <c>ID3D11Device1::OpenSharedResource1</c>.</summary>
    D3D11NtHandle,

    /// <summary>An <c>IOSurfaceRef</c> holding BGRA8 pixels, top row first.</summary>
    IOSurface,
}

/// <summary>A GPU texture another API or process hands the canvas, valid only for the call it is passed
/// to: the canvas copies it into a texture of its own before returning. <paramref name="WidthPx"/> and
/// <paramref name="HeightPx"/> are the size of what it shows, in pixels, when the sender knows it; 0 when not.</summary>
public readonly record struct SharedTexture(SharedTextureKind Kind, nint Handle, int WidthPx = 0, int HeightPx = 0);
