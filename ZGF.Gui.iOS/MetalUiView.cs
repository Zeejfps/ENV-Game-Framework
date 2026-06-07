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
//
// Text handling: the same view is the keyboard's first responder. Rather than hosting a hidden
// UITextField, it conforms to UIKeyInput directly (the supported way to drive the keyboard from a
// custom-drawn surface — what editors and game engines do): focusing it raises the keyboard and
// routes keystrokes to the active ITextInputClient, which owns its own text buffer. It backs the
// platform-neutral ITextInputService, the text parallel to MobileInputSystem.
//
// Keyboard avoidance is NOT done here: this view just reports the keyboard's covered height to the
// shared KeyboardInsets, and the framework's scroll container does the scrolling. No view shifting.
public sealed class MetalUiView : UIView, IUIKeyInput, IUITextInputTraits, ITextInputService
{
    [Export("layerClass")]
    public static Class GetLayerClass() => new Class(typeof(CAMetalLayer));

    private ITextInputClient? _textClient;
    private NSObject? _keyboardToken;
    private CGRect _keyboardScreenFrame = CGRect.Empty;

    public MetalUiView(CGRect frame) : base(frame)
    {
        MultipleTouchEnabled = false;
        UserInteractionEnabled = true;

        // Report the keyboard's covered height to the framework as it changes.
        _keyboardToken = UIKeyboard.Notifications.ObserveWillChangeFrame((_, e) =>
        {
            _keyboardScreenFrame = e.FrameEnd;
            ReportKeyboardInset();
        });
    }

    public CAMetalLayer MetalLayer => (CAMetalLayer)Layer;

    /// <summary>The shared touch input system to drive; set by the view controller once built.</summary>
    public MobileInputSystem? Input { get; set; }

    /// <summary>Keyboard inset sink; set by the view controller so the framework can avoid the keyboard.</summary>
    public KeyboardInsets? Insets { get; set; }

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

    // --- Keyboard (ITextInputService + UIKeyInput) ----------------------------------------

    public override bool CanBecomeFirstResponder => true;

    // No platform input-accessory view: the "Done" bar is drawn by the framework
    // (ZGF.Gui.Mobile.Input.KeyboardAccessoryBar) so it's themed like the rest of the UI.

    public void BeginEdit(ITextInputClient client)
    {
        // Switching focus: let the previously-edited field commit and drop its active state.
        if (!ReferenceEquals(_textClient, client))
            _textClient?.OnEditingEnded();

        _textClient = client;
        KeyboardType = ToKeyboardType(client.Keyboard);
        if (IsFirstResponder)
            ReloadInputViews(); // switching fields: update the keyboard type in place
        else
            BecomeFirstResponder();
    }

    public void EndEdit(ITextInputClient client)
    {
        if (_textClient == client)
            ResignFirstResponder();
    }

    public override bool ResignFirstResponder()
    {
        var result = base.ResignFirstResponder();
        var client = _textClient;
        _textClient = null;
        client?.OnEditingEnded();
        return result;
    }

    // UIKeyInput: the keyboard calls these on the first responder; we forward to the active client.
    public bool HasText => _textClient?.HasText ?? false;
    public void InsertText(string text) => _textClient?.InsertText(text);
    public void DeleteBackward() => _textClient?.DeleteBackward();

    // UITextInputTraits: the keyboard reads this off the first responder to pick its layout.
    [Export("keyboardType")]
    public UIKeyboardType KeyboardType { get; set; } = UIKeyboardType.Default;

    // Translate the keyboard frame into the covered height (canvas points) and hand it to the
    // framework. The framework's scroll container reserves that space and scrolls the focused field
    // clear — no view shifting here.
    private void ReportKeyboardInset()
    {
        if (Insets == null)
            return;

        var inset = 0f;
        if (!_keyboardScreenFrame.IsEmpty)
        {
            var keyboardTop = (float)ConvertRectFromView(_keyboardScreenFrame, null).Y;
            var viewHeight = (float)Bounds.Height;
            inset = MathF.Max(0f, viewHeight - keyboardTop);
        }

        Insets.SetBottom(inset);
    }

    private static UIKeyboardType ToKeyboardType(TextInputKeyboard keyboard) => keyboard switch
    {
        TextInputKeyboard.Number => UIKeyboardType.NumberPad,
        TextInputKeyboard.Decimal => UIKeyboardType.DecimalPad,
        _ => UIKeyboardType.Default,
    };

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _keyboardToken?.Dispose();
            _keyboardToken = null;
        }
        base.Dispose(disposing);
    }
}
