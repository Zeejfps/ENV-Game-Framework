using Foundation;
using UIKit;

namespace ZGF.Gui.iOS;

// Scene-based lifecycle (iOS 13+). The window is created from its UIWindowScene rather than the
// deprecated UIScreen.MainScreen + windowless UIWindow path. Referenced from Info.plist's
// UIApplicationSceneManifest by its registered name "SceneDelegate".
[Register("SceneDelegate")]
public sealed class SceneDelegate : UIResponder, IUIWindowSceneDelegate
{
    [Export("window")]
    public UIWindow? Window { get; set; }

    [Export("scene:willConnectToSession:options:")]
    public void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        if (scene is not UIWindowScene windowScene)
            return;

        Window = new UIWindow(windowScene)
        {
            RootViewController = new MetalViewController(),
        };
        Window.MakeKeyAndVisible();
    }
}
