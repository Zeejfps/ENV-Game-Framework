namespace Xilium.CefGlue
{
    using System;

    /// <summary>
    /// The shared texture of one OnAcceleratedPaint call. On Windows the handle is an NT handle to a
    /// D3D11 texture without a keyed mutex; it is only valid during the call.
    /// </summary>
    public readonly struct CefAcceleratedPaintInfo
    {
        public CefAcceleratedPaintInfo(IntPtr sharedTextureHandle, CefColorType format, CefRectangle visibleRect, CefSize codedSize)
        {
            SharedTextureHandle = sharedTextureHandle;
            Format = format;
            VisibleRect = visibleRect;
            CodedSize = codedSize;
        }

        public IntPtr SharedTextureHandle { get; }
        public CefColorType Format { get; }
        public CefRectangle VisibleRect { get; }
        public CefSize CodedSize { get; }
    }
}
