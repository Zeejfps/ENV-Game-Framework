namespace ZGF.Gui.Desktop;

/// <summary>
/// Fallback <see cref="ISecretStore"/> for platforms without a credential store. Reads report "no
/// secret" silently so the caller's own fallback takes over; writes log and are dropped, since a
/// silently discarded secret would otherwise look saved.
/// </summary>
public sealed class NoopSecretStore : ISecretStore
{
    public string? Get(string name) => null;

    public bool Set(string name, string secret)
    {
        Console.WriteLine($"[SecretStore] No native secret store for this OS; '{name}' was not saved.");
        return false;
    }

    public bool Delete(string name) => true;
}
