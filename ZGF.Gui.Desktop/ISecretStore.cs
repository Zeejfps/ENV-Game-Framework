namespace ZGF.Gui.Desktop;

/// <summary>
/// Keeps small user secrets — API keys, tokens — in the OS credential store. Register a platform
/// implementation with <see cref="SecretStoreServices.AddNativeSecretStore"/>, then resolve
/// <c>ISecretStore</c> from the <see cref="Context"/> where a credential is read or saved. Every
/// operation reports failure as a null/false result: an absent, locked, or refusing OS store is an
/// ordinary outcome the caller falls back from, not an exception to guard.
/// </summary>
public interface ISecretStore
{
    /// <summary>
    /// Reads the secret stored under <paramref name="name"/>; null when there is none or the OS
    /// store could not answer. Blocks the calling thread until the OS responds, which on macOS and
    /// Linux can include an unlock prompt, so prefer a worker thread over the UI thread.
    /// </summary>
    string? Get(string name);

    /// <summary>
    /// Stores <paramref name="secret"/> under <paramref name="name"/>, replacing any previous
    /// value. Returns false when <paramref name="secret"/> is empty or the OS store rejected the
    /// write; nothing is stored in that case.
    /// </summary>
    bool Set(string name, string secret);

    /// <summary>
    /// Removes the secret stored under <paramref name="name"/>. Returns true whenever no secret
    /// remains under that name, including when there was none to begin with.
    /// </summary>
    bool Delete(string name);
}
