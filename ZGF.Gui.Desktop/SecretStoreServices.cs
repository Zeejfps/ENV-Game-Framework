using System.Runtime.InteropServices;
using ZGF.Gui.Desktop.Platforms.Linux;
using ZGF.Gui.Desktop.Platforms.Osx;
using ZGF.Gui.Desktop.Platforms.Windows;

namespace ZGF.Gui.Desktop;

/// <summary>
/// Registers the OS-native <see cref="ISecretStore"/> on a <see cref="Context"/>. Call it once
/// during app setup, before resolving <c>ISecretStore</c>.
/// </summary>
public static class SecretStoreServices
{
    extension(Context context)
    {
        /// <summary>
        /// Registers the credential store for the current OS: Windows DPAPI, the macOS Keychain,
        /// or the Linux Secret Service. Falls back to a <see cref="NoopSecretStore"/> on any other
        /// platform. <paramref name="serviceName"/> namespaces this app's secrets within the OS
        /// store and must stay stable across releases, or previously saved secrets stop resolving.
        /// </summary>
        public void AddNativeSecretStore(string serviceName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                context.AddService<ISecretStore>(new WindowsSecretStore(serviceName));
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                context.AddService<ISecretStore>(new MacOsSecretStore(serviceName));
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                context.AddService<ISecretStore>(new LinuxSecretStore(serviceName));
            else
                context.AddService<ISecretStore>(new NoopSecretStore());
        }
    }
}
