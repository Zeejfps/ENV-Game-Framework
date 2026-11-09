/// <summary>The right to fill and use <see cref="GL46"/>'s entry points, which are static and so shared by the whole
/// process: one owner at a time, from before it imports them until its last context is destroyed. Disposing gives
/// the right up.</summary>
public sealed class GlEntryPointOwner : IDisposable
{
    // Process-wide by necessity: GL46's entry points are static fields, so only one owner may hold them at a time.
    private static GlEntryPointOwner? s_holder;

    private GlEntryPointOwner(string name)
    {
        Name = name;
    }

    /// <summary>Who holds the entry points, for the message a refused claim gives.</summary>
    public string Name { get; }

    public bool IsHeld => Volatile.Read(ref s_holder) == this;

    /// <summary>Claims the entry points for <paramref name="name"/>.</summary>
    /// <exception cref="InvalidOperationException">Another owner holds them.</exception>
    public static GlEntryPointOwner Claim(string name)
    {
        var owner = new GlEntryPointOwner(name);
        var held = Interlocked.CompareExchange(ref s_holder, owner, null);
        if (held != null)
            throw new InvalidOperationException(
                $"Cannot open {name} while {held.Name} is already live in this process: GL entry points are process-wide, so dispose it first.");
        return owner;
    }

    internal void ThrowIfNotHeld()
    {
        if (!IsHeld)
            throw new InvalidOperationException($"{Name} no longer holds the process's GL entry points, so it cannot import them.");
    }

    /// <summary>Gives the entry points up; disposing again, or after another owner claimed them, changes nothing.</summary>
    public void Dispose() => Interlocked.CompareExchange(ref s_holder, null, this);
}
