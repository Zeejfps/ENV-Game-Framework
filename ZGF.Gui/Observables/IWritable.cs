namespace ZGF.Observable;

/// <summary>
/// The write side of an observable value, layered on <see cref="IReadable{T}"/>. A two-way
/// widget input reads through the inherited getter and pushes edits back through the setter,
/// so a binding can be two-way without naming the concrete <see cref="State{T}"/> storage type.
/// </summary>
public interface IWritable<T> : IReadable<T>
{
    new T Value { get; set; }
}
