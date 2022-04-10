namespace Framework.InputDevices;

public interface IMouse
{
    public int ScreenX { get; }
    public int ScreenY { get; }

    public void PressButton(MouseButton button);
    public void ReleaseButton(MouseButton button);
    public bool WasButtonPressedThisFrame(MouseButton button);
    public bool WasButtonReleasedThisFrame(MouseButton button);
    public bool IsButtonPressed(MouseButton button);
    public bool IsButtonReleased(MouseButton button);
}

public readonly struct MouseButton : IEquatable<MouseButton>
{
    public static readonly MouseButton Left = new(0);
    public static readonly MouseButton Right = new(1);
    public static readonly MouseButton Middle = new(3);
    
    private int ButtonId { get; }

    public MouseButton(int id)
    {
        ButtonId = id;
    }

    public bool Equals(MouseButton other)
    {
        return ButtonId == other.ButtonId;
    }

    public override bool Equals(object? obj)
    {
        return obj is MouseButton other && Equals(other);
    }

    public override int GetHashCode()
    {
        return ButtonId;
    }

    public static bool operator ==(MouseButton left, MouseButton right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MouseButton left, MouseButton right)
    {
        return !left.Equals(right);
    }
}