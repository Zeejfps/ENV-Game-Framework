namespace ZGF.Gui;


public readonly record struct MouseButton
{
    public static readonly MouseButton Left = new(0);
    public static readonly MouseButton Right = new(1);
    public static readonly MouseButton Middle = new(3);

    private int ButtonId { get; }

    public MouseButton(int id)
    {
        ButtonId = id;
    }

    public override string ToString()
    {
        if (ButtonId == Left.ButtonId)
            return $"Button[{ButtonId}] - Left";
        if (ButtonId == Right.ButtonId)
            return $"Button[{ButtonId}] - Right";
        if (ButtonId == Middle.ButtonId)
            return $"Button[{ButtonId}] - Middle";

        return $"[{ButtonId}]";
    }
}