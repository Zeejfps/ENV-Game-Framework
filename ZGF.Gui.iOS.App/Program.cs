using UIKit;

namespace ZGF.Gui.iOS.App;

public static class Program
{
    private static void Main(string[] args)
    {
        // UIApplicationMain takes over the main thread and pumps the iOS run loop,
        // instantiating the AppDelegate named below to drive the app lifecycle.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
