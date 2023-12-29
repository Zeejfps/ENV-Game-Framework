using EasyGameFramework.Builder;

namespace Tetris;

public sealed class Context
{
    private readonly DiContainer m_Container;
    private readonly List<IEntity> m_Entities = new();
    private readonly List<IEntityFactory> m_EntityFactories = new();
    
    public Context()
    {
        m_Container = new DiContainer();
        RegisterSingleton<Context>(this);
    }

    public Context(Context parent)
    {
        m_Container = new DiContainer(parent.m_Container);
        RegisterSingleton<Context>(this);
    }
    
    public void RegisterSingleton<TInterface>(TInterface instance)
    {
        m_Container.BindSingleton(instance);
    }
    
    public void RegisterSingleton<TInterface, TConcrete>() where TConcrete : TInterface
    {
        m_Container.BindSingleton<TInterface, TConcrete>();
    }
    
    public void RegisterSingletonEntity<TInterface, TConcrete>() 
        where TConcrete : class, TInterface, IEntity
    {
        m_Container.BindSingleton<TInterface, TConcrete>();
        m_EntityFactories.Add(new SingletonEntityFactory<TInterface, TConcrete>(m_Container));
    }
    
    public void RegisterTransientEntity<T>() where T : IEntity
    {
        m_EntityFactories.Add(new TransientEntityFactory<T>(m_Container));
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