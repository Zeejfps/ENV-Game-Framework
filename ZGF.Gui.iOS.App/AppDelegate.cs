using Foundation;
using UIKit;

namespace ZGF.Gui.iOS.App;

// Hosts the Metal-backed GUI view controller. The CAMetalLayer canvas rendering is set up
// in MetalViewController; this just creates the window and makes it visible.
[Register("AppDelegate")]
public sealed class AppDelegate : UIApplicationDelegate
{
    public override UIWindow? Window { get; set; }

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        Window = new UIWindow(UIScreen.MainScreen.Bounds)
        {
            RootViewController = new MetalViewController(),
        };
        Window.MakeKeyAndVisible();
        return true;
    }
}
