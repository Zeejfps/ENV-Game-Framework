using EasyGameFramework.Api;
using EasyGameFramework.Builder;

namespace Tetris;

public sealed class TestEntity
{

    public TestEntity()
    {

    }
}

public interface IEntity
{
    void Load();
    void Unload();
}

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