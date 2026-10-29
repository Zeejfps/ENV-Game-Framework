using System.Collections.Immutable;

namespace ZGF.ILSpy.Core;

/// <summary>
/// What the user picked for each assembly of a list, by the assembly's path, such as the PDB it reads
/// or the framework its references are resolved against. An assembly takes its pick when it is opened
/// next. Kept when the list is cleared, as a reload does; forgotten with the assembly when it is
/// closed. Safe from any thread.
/// </summary>
public sealed class AssemblyPicks
{
    private static readonly ImmutableDictionary<AssemblyPath, string> None = ImmutableDictionary<AssemblyPath, string>.Empty;

    private readonly Lock _gate = new();
    private ImmutableDictionary<AssemblyPath, string> _picks = None;

    /// <summary>Every pick, by the assembly's path.</summary>
    public IReadOnlyDictionary<AssemblyPath, string> All => Volatile.Read(ref _picks);

    /// <summary>Has the assembly at <paramref name="assemblyPath"/> take <paramref name="value"/> from
    /// the next time it is opened.</summary>
    public void Pick(AssemblyPath assemblyPath, string value)
    {
        lock (_gate)
            Volatile.Write(ref _picks, _picks.SetItem(assemblyPath, value));
    }

    /// <summary>Has the assembly at <paramref name="assemblyPath"/> find for itself again what it was
    /// given, from the next time it is opened.</summary>
    public void Forget(AssemblyPath assemblyPath)
    {
        lock (_gate)
            Volatile.Write(ref _picks, _picks.Remove(assemblyPath));
    }

    /// <summary>Takes <paramref name="picks"/> in place of those before, as when another list opens.</summary>
    public void Use(IReadOnlyDictionary<AssemblyPath, string> picks)
    {
        var taken = ImmutableDictionary.CreateRange(picks);
        lock (_gate)
            Volatile.Write(ref _picks, taken);
    }

    /// <summary>Forgets every pick, as when the list is closed for good.</summary>
    public void ForgetAll()
    {
        lock (_gate)
            Volatile.Write(ref _picks, None);
    }

    /// <summary>What was picked for the assembly at <paramref name="assemblyPath"/>, or null.</summary>
    public string? Find(AssemblyPath assemblyPath)
    {
        return All.GetValueOrDefault(assemblyPath);
    }

    /// <summary>Forgets what was picked for <paramref name="item"/> and, for a package, every assembly
    /// in it.</summary>
    internal void ForgetFor(LoadedAssembly item)
    {
        lock (_gate)
        {
            var picks = _picks.Remove(item.FilePath);
            if (item is LoadedPackage)
                picks = picks.RemoveRange(picks.Keys.Where(path => path.IsUnder(item.FilePath)));
            Volatile.Write(ref _picks, picks);
        }
    }
}
