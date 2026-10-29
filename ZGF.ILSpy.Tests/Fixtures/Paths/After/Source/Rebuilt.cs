namespace PathsSample;

public class Box<TItem>
{
    public TItem? Value;

    public void Put(TItem item) => Value = item;
}

public record Shape;

public static class Limits
{
    public const int Max = 5;
}

public class Added
{
}

public class AddedToo
{
}
