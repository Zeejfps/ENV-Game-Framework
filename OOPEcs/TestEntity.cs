using EasyGameFramework.Api;
using EasyGameFramework.Builder;

namespace Tetris;

public sealed class TestEntity
{

    public TestEntity()
    {
        var worldBuilder = new WorldBuilder()
            .WithEntity<SuperTestEntity>();

        var world = worldBuilder.Build();
        world.Load();
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


public interface IClock
{
    
}

public sealed class WorldBuilder
{
    private readonly List<IEntityFactory> m_EntityFactories = new();
    private readonly DiContainer m_DiContainer = new();
    
    public IEntity Build()
    {
        var entities = m_EntityFactories
            .Select(entityFactory => entityFactory.Create());
        return new World(entities);
    }
    
    public WorldBuilder WithEntity<T>() where T : IEntity
    {
        var factory = new ConcreteEntityFactory<T>(m_DiContainer);
        m_EntityFactories.Add(factory);
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


public sealed class World : IEntity
{
    private readonly List<IEntity> m_Entities;

    public World(IEnumerable<IEntity> entities)
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