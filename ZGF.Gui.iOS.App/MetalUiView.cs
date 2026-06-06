using CoreAnimation;
using CoreGraphics;
using ObjCRuntime;
using UIKit;

namespace ZGF.Gui.iOS.App;

// A UIView whose backing layer is a CAMetalLayer. Overriding +layerClass is the supported
// way to get a Metal-backed view: UIKit creates the layer, manages its lifetime, and keeps
// it sized/positioned with the view. We render into this layer's drawables each frame.
public sealed class MetalUiView : UIView
{
    [Export("layerClass")]
    public static Class GetLayerClass() => new Class(typeof(CAMetalLayer));

    public MetalUiView(CGRect frame) : base(frame)
    {
    }

    public CAMetalLayer MetalLayer => (CAMetalLayer)Layer;
}
