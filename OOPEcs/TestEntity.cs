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


public sealed class WorldBuilder
{
    private readonly List<IEntityFactory> m_EntityFactories = new();
    private readonly DiContainer m_DiContainer = new();
    
    public IEntity Build()
    {
        var entities = m_EntityFactories
            .Select(entityFactory => entityFactory.Create());
        return new WorldTest(entities);
    }
    
    public WorldBuilder WithEntity<T>() where T : IEntity
    {
        var factory = new ConcreteEntityFactory<T>(m_DiContainer);
        m_EntityFactories.Add(factory);
        return this;
    }

    public WorldBuilder WithSingleton<T, TImpl>() where TImpl : T
    {
        m_DiContainer.BindSingleton<T, TImpl>();
        return this;
    }
}

sealed class ConcreteEntityFactory<T> : IEntityFactory where T : IEntity
{
    private readonly IContainer m_Container;

    public ConcreteEntityFactory(IContainer container)
    {
        m_Container = container;
    }

    public IEntity Create()
    {
        return m_Container.New<T>();
    }
}

public interface IEntityFactory
{
    IEntity Create();
}

public interface IWorld : IEntity
{
    
}


public sealed class WorldTest : IEntity
{
    private readonly List<IEntity> m_Entities;

    public WorldTest(IEnumerable<IEntity> entities)
    {
        m_Entities = entities.ToList();
    }

    public void Load()
    {
        foreach (var entity in m_Entities)
        {
            entity.Load();
        }
    }

    public void Unload()
    {
        foreach (var entity in m_Entities)
        {
            entity.Unload();
        }
    }
}