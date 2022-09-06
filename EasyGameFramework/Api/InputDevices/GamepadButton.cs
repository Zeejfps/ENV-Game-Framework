namespace EasyGameFramework.Api.InputDevices;

public readonly struct GamepadButton : IEquatable<GamepadButton>
{
    public static readonly GamepadButton South = new(0);
    public static readonly GamepadButton East = new(1);
    public static readonly GamepadButton West = new(2);
    public static readonly GamepadButton North = new(3);
    
    public static readonly GamepadButton LeftBumper = new(4);
    public static readonly GamepadButton RightBumper = new(5);

    public static readonly GamepadButton Back = new(6);
    public static readonly GamepadButton Start = new(7);
    public static readonly GamepadButton Guide = new(8);
    
    public static readonly GamepadButton LeftThumb = new(9);
    public static readonly GamepadButton RightThumb = new(10);
    
    public static readonly GamepadButton DPadUp = new(11);
    public static readonly GamepadButton DPadLeft = new(12);
    public static readonly GamepadButton DPadRight = new(13);
    public static readonly GamepadButton DPadDown = new(14);

    private readonly int m_Id;
    
    public GamepadButton(int id)
    {
        m_Id = id;
    }

    public bool Equals(GamepadButton other)
    {
        return m_Id == other.m_Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is GamepadButton other && Equals(other);
    }

    public override int GetHashCode()
    {
        return m_Id;
    }

    public static bool operator ==(GamepadButton left, GamepadButton right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GamepadButton left, GamepadButton right)
    {
        return !left.Equals(right);
    }
}