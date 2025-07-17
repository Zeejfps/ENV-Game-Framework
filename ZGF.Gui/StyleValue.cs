namespace ZGF.Gui;

public struct StyleValue<T>
{
    public StyleValue(T? value)
    {
        Value = value;
    }
    
    public StyleValue(T? value, bool isSet)
    {
        Value = value;
        IsSet = isSet;
    }

    public bool IsSet { get; set; } = true;
    public T? Value { get; set; }
    
    public void Reset()
    {
        Value = default;
        IsSet = false;
    }

    public static implicit operator StyleValue<T> (T value) => new(value);
    public static implicit operator T?(StyleValue<T> value) => value.Value;
    
    public static StyleValue<T> Unset => new(default, false);
}