namespace ZGF.ILSpy.Tests.Fixtures;

public class Calculator<T>
{
    public const int Answer = 42;

    private readonly List<T> _items = new();

    public Calculator()
    {
    }

    public event EventHandler? Changed;

    /// <summary>How many items were added.</summary>
    public int Count => _items.Count;

    public T this[int index] => _items[index];

    /// <summary>Adds an item and raises <see cref="Changed"/>.</summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item)
    {
        _items.Add(item);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool TryGet(int index, out T? item)
    {
        item = index < _items.Count ? _items[index] : default;
        return index < _items.Count;
    }

    public static Calculator<T> operator +(Calculator<T> calculator, T item)
    {
        calculator.Add(item);
        return calculator;
    }

    [System.ComponentModel.Description("Summarizes the calculator")]
    internal string Describe()
    {
        var total = Count * 2 + Answer;
        return "Total: " + total;
    }

    public enum Mode
    {
        Fast,
        Slow,
        Precise,
    }
}

public interface IShape
{
    /// <summary>The space the shape covers.</summary>
    double Area { get; }
}

public struct Size
{
    public int Width;
    public int Height;
}

public record Point(int X, int Y);

public delegate void Callback(int value);

internal sealed class Circle : IShape
{
    public double Radius { get; init; }

    /// <inheritdoc/>
    public double Area => Math.PI * Radius * Radius;

    /// <summary>Multiplies the radius by <paramref name="factor"/>, <c>times</c> over.</summary>
    /// <remarks>
    /// <para>Each step multiplies once:</para>
    /// <list type="bullet">
    /// <item><term>Zero</term><description>keeps the radius.</description></item>
    /// <item><description>More grows it.</description></item>
    /// </list>
    /// <code>
    ///     var grown = circle.Grow(2, 3);
    ///     Console.WriteLine(grown);
    /// </code>
    /// </remarks>
    /// <returns>The grown radius, <see langword="true"/> to <see href="https://example.com">the example</see>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is negative.</exception>
    public double Grow(double factor, int times)
    {
        var grown = Radius;
        for (var step = 0; step < times; step++)
            grown *= factor;
        return grown;
    }

    public double Spread()
    {
        (double Low, double High) range = (Radius, Radius * 2);
        return range.High - range.Low;
    }

    public object Echo(object value)
    {
        dynamic echoed = value;
        return echoed;
    }
}
