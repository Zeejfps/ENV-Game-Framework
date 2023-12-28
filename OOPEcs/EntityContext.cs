using EasyGameFramework.Api;
using EasyGameFramework.Builder;

namespace Tetris;

public sealed class EntityContext : IEntity
{
    private IContainer Container { get; } = new DiContainer();

    private readonly List<IEntity> m_Entities = new();
    private readonly List<IEntityFactory> m_EntityFactories = new();

    public EntityContext()
    {
        RegisterSingleton<EntityContext>(this);
    }
    
    public void RegisterSingleton<TInterface>(TInterface instance)
    {
        Container.BindSingleton(instance);
    }
    
    public void RegisterSingleton<TInterface, TConcrete>() where TConcrete : TInterface
    {
        Container.BindSingleton<TInterface, TConcrete>();
    }
    
    public void RegisterSingletonEntity<TInterface, TConcrete>() 
        where TConcrete : class, TInterface, IEntity
    {
        Container.BindSingleton<TInterface, TConcrete>();
        m_EntityFactories.Add(new SingletonEntityFactory<TInterface, TConcrete>(Container));
    }
    
    public void RegisterTransientEntity<T>() where T : IEntity
    {
        m_EntityFactories.Add(new ConcreteEntityFactory<T>(Container));
    }

    public void Load()
    {
        var entities = m_EntityFactories.Select(factory => factory.Create());
        m_Entities.AddRange(entities);
        foreach (var entity in m_Entities)
            entity.Load();
    }

    public void Unload()
    {
        foreach (var entity in m_Entities)
            entity.Unload();
        m_Entities.Clear();
    }
}

sealed class SingletonEntityFactory<TInterface, TConcrete> : IEntityFactory where TConcrete : class, IEntity
{
    private TConcrete? m_Instance;
    private readonly IContainer m_Container;

    public SingletonEntityFactory(IContainer container)
    {
        m_Container = container;
    }

    public IEntity Create()
    {
        if (m_Instance == null)
            m_Instance = m_Container.New<TInterface>() as TConcrete;
        return m_Instance;
    }
}