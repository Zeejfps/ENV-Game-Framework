namespace ZGF.Gui;

public sealed class Style
{
    public PaddingStyle Padding { get; set; }
}

public struct StyleValue<T>(T value)
{
    public bool IsSet { get; set; } = true;
    public T Value { get; set; } = value;

    public static implicit operator StyleValue<T> (T value) => new(value);
    public static implicit operator T(StyleValue<T> value) => value.Value;
}