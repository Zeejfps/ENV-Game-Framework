using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>
/// The tooltips of references hovered in code: each is looked up once, in the background, and kept
/// while the open assemblies stay the same. A lookup that finishes after they changed is dropped.
/// </summary>
public sealed class CodeTooltips
{
    private readonly AssemblyList _assemblies;
    private readonly Dictionary<CodeReference, CodeTooltip?> _found = new();
    private readonly HashSet<CodeReference> _pending = new();
    private int _version;

    public CodeTooltips(AssemblyList assemblies)
    {
        _assemblies = assemblies;
        _version = assemblies.Version;
    }

    /// <summary>Whether a lookup is still running in the background.</summary>
    public bool IsLookingUp => _pending.Count > 0;

    /// <summary>Whether the lookup of <paramref name="reference"/> is done, with its
    /// <paramref name="tooltip"/>, null when it has none. Starts the lookup the first time.</summary>
    public bool TryGet(CodeReference reference, out CodeTooltip? tooltip)
    {
        ForgetIfStale();
        if (_found.TryGetValue(reference, out tooltip))
            return true;
        LookUp(reference);
        return false;
    }

    private void ForgetIfStale()
    {
        if (_version == _assemblies.Version)
            return;
        _version = _assemblies.Version;
        _found.Clear();
        _pending.Clear();
    }

    private async void LookUp(CodeReference reference)
    {
        if (!_pending.Add(reference))
            return;
        var version = _version;
        var outcome = await Background.Run(() => _assemblies.Describe(reference));
        if (version != _version)
            return;
        _pending.Remove(reference);
        _found[reference] = outcome.Value;
    }
}
