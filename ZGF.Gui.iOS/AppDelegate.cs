using Foundation;
using UIKit;

namespace ZGF.Gui.iOS;

// App-level lifecycle only. Window creation moved to SceneDelegate under the scene-based
// lifecycle; the scene is wired up via Info.plist's UIApplicationSceneManifest.
[Register("AppDelegate")]
public sealed class AppDelegate : UIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions) => true;
}
