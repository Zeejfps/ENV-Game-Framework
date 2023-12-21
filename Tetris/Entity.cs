using System.Numerics;

namespace Tetris;

public struct Entity
{
    public Vector2 Position;

    private Flag m_Flags = Flag.None;
    
    public Entity() {}

    public void Set(Flag flag)
    {
        m_Flags |= flag;
    }

    public void Clear(Flag flag)
    {
        m_Flags &= ~flag;
    }

    public bool Is(Flag flag)
    {
        return m_Flags == flag;
    }

    public bool Has(Flag flag)
    {
        return m_Flags.HasFlag(flag);
    }
}