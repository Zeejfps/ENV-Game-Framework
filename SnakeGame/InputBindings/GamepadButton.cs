namespace SampleGames;

public readonly struct GamepadButton
{
    public static readonly GamepadButton South = new(0);
    public static readonly GamepadButton Xbox_A = new(0);
    public static readonly GamepadButton PS_Circle = new(0);

    private readonly int m_Id;
    
    public GamepadButton(int id)
    {
        m_Id = id;
    }
}