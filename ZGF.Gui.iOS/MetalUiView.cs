using CoreAnimation;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;
using ZGF.Gui.Mobile.Input;

namespace ZGF.Gui.iOS;

// A UIView whose backing layer is a CAMetalLayer. Overriding +layerClass is the supported
// way to get a Metal-backed view: UIKit creates the layer, manages its lifetime, and keeps
// it sized/positioned with the view. We render into this layer's drawables each frame.
//
// Touch handling: this is the platform glue for ZGF.Gui.Mobile. UIKit delivers touches here;
// we forward the primary touch's location (logical points, top-left origin) to the shared
// MobileInputSystem, which flips Y into canvas space and dispatches to the view tree. All the
// per-platform code lives in these four overrides — the routing itself is shared with Android.
public sealed class MetalUiView : UIView
{
    [Export("layerClass")]
    public static Class GetLayerClass() => new Class(typeof(CAMetalLayer));

    public MetalUiView(CGRect frame) : base(frame)
    {
        MultipleTouchEnabled = false;
        UserInteractionEnabled = true;
    }

    public CAMetalLayer MetalLayer => (CAMetalLayer)Layer;

    /// <summary>The shared touch input system to drive; set by the view controller once built.</summary>
    public MobileInputSystem? Input { get; set; }

    public override void TouchesBegan(NSSet touches, UIEvent? evt)
    {
        if (TryGetPoint(touches, out var x, out var y))
            Input?.OnPointerDown(x, y);
    }

    public override void TouchesMoved(NSSet touches, UIEvent? evt)
    {
        if (TryGetPoint(touches, out var x, out var y))
            Input?.OnPointerMoved(x, y);
    }

    public override void TouchesEnded(NSSet touches, UIEvent? evt)
    {
        if (TryGetPoint(touches, out var x, out var y))
            Input?.OnPointerUp(x, y);
    }

    public override void TouchesCancelled(NSSet touches, UIEvent? evt)
    {
        Input?.OnPointerCancelled();
    }

    private bool TryGetPoint(NSSet touches, out float x, out float y)
    {
        if (touches.AnyObject is UITouch touch)
        {
            var location = touch.LocationInView(this);
            x = (float)location.X;
            y = (float)location.Y;
            return true;
        }
        x = 0f;
        y = 0f;
        return false;
    }
}
