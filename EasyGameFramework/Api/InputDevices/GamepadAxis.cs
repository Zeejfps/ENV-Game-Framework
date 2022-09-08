namespace EasyGameFramework.Api.InputDevices;

public readonly struct GamepadAxis : IEquatable<GamepadAxis>
{
    public static readonly GamepadAxis LeftStickX = new(0);
    public static readonly GamepadAxis LeftStickY = new(1);

    private readonly int m_Id;

    public GamepadAxis(int id)
    {
        m_Id = id;
    }

    public bool Equals(GamepadAxis other)
    {
        return m_Id == other.m_Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is GamepadAxis other && Equals(other);
    }

    public override int GetHashCode()
    {
        return m_Id;
    }

    public static bool operator ==(GamepadAxis left, GamepadAxis right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GamepadAxis left, GamepadAxis right)
    {
        return !left.Equals(right);
    }
}