using EasyGameFramework.Api;

namespace Tetris;

public sealed class SuperTestEntity : IEntity
{
    private readonly IClock m_Clock;

    public SuperTestEntity(IClock clock)
    {
        m_Clock = clock;
    }

    public void Load()
    {
        
    }

    public void Unload()
    {
    }
}