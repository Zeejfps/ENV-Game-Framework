using System.Runtime.InteropServices;
using ZGF.Gui.Desktop;

namespace ZGF.Gui.Tests;

/// <summary>
/// Exercises the native secret store against the real OS credential store. The round-trip cases
/// run only where the backing store is guaranteed present (DPAPI on Windows); elsewhere they
/// return early rather than failing a machine without a keychain or secret-tool.
/// </summary>
public class SecretStoreTests
{
    private static bool CanRoundTrip => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    [Fact]
    public void NoopStoreReportsNoSecret()
    {
        var store = new NoopSecretStore();

        Assert.False(store.Set("api-key", "sk-secret"));
        Assert.Null(store.Get("api-key"));
        Assert.True(store.Delete("api-key"));
    }

    [Fact]
    public void RegistersAStoreForTheCurrentPlatform()
    {
        using var context = new Context();
        context.AddNativeSecretStore("ZGF.Gui.Tests");

        Assert.NotNull(context.Require<ISecretStore>());
    }

    [Fact]
    public void MissingSecretReadsAsNull()
    {
        using var scope = new StoreScope();

        Assert.Null(scope.Store.Get("never-stored"));
    }

    [Fact]
    public void EmptySecretIsRejected()
    {
        using var scope = new StoreScope();

        Assert.False(scope.Store.Set("api-key", ""));
        Assert.Null(scope.Store.Get("api-key"));
    }

    [Fact]
    public void RoundTripsASecret()
    {
        if (!CanRoundTrip) return;

        using var scope = new StoreScope();

        Assert.True(scope.Store.Set("api-key", "sk-ant-é中-value"));
        Assert.Equal("sk-ant-é中-value", scope.Store.Get("api-key"));

        Assert.True(scope.Store.Set("api-key", "replaced"));
        Assert.Equal("replaced", scope.Store.Get("api-key"));

        Assert.True(scope.Store.Delete("api-key"));
        Assert.Null(scope.Store.Get("api-key"));
    }

    [Fact]
    public void SecretsAreKeyedByName()
    {
        if (!CanRoundTrip) return;

        using var scope = new StoreScope();

        Assert.True(scope.Store.Set("first", "one"));
        Assert.True(scope.Store.Set("second", "two"));

        Assert.Equal("one", scope.Store.Get("first"));
        Assert.Equal("two", scope.Store.Get("second"));

        Assert.True(scope.Store.Delete("first"));
        Assert.Null(scope.Store.Get("first"));
        Assert.Equal("two", scope.Store.Get("second"));
    }

    [Fact]
    public void NamesWithPathCharactersStayInsideTheStore()
    {
        if (!CanRoundTrip) return;

        using var scope = new StoreScope();

        Assert.True(scope.Store.Set("../escape/attempt", "value"));
        Assert.Equal("value", scope.Store.Get("../escape/attempt"));
        Assert.True(Directory.Exists(scope.WindowsRoot));
        Assert.True(scope.Store.Delete("../escape/attempt"));
    }

    [Fact]
    public void DeletingAMissingSecretSucceeds()
    {
        if (!CanRoundTrip) return;

        using var scope = new StoreScope();

        Assert.True(scope.Store.Delete("never-stored"));
    }

    /// <summary>
    /// A store under a service name no other run shares, so the tests never see or disturb real
    /// secrets, plus removal of what the Windows backend wrote to disk.
    /// </summary>
    private sealed class StoreScope : IDisposable
    {
        private readonly Context _context = new();
        private readonly string _serviceName = "ZGF.Gui.Tests." + Guid.NewGuid().ToString("N");

        public StoreScope()
        {
            _context.AddNativeSecretStore(_serviceName);
        }

        public ISecretStore Store => _context.Require<ISecretStore>();

        public string WindowsRoot => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            _serviceName);

        public void Dispose()
        {
            _context.Dispose();
            if (Directory.Exists(WindowsRoot)) Directory.Delete(WindowsRoot, recursive: true);
        }
    }
}
