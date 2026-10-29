namespace PathsSample;

public class Box<T>
{
    public T? Value;

    public void Put(T item) => Value = item;
}

public class Shape
{
}

public static class Limits
{
    public static int Max = 5;
}
