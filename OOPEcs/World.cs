using EasyGameFramework.Api;
using EasyGameFramework.Builder;

namespace Tetris;

public abstract class World : IEntity
{
    private IContainer Container { get; } = new DiContainer();

    private readonly List<IEntity> m_Entities = new();
    private readonly List<IEntityFactory> m_EntityFactories = new();

    protected void RegisterSingleton<TInterface>(TInterface instance)
    {
        Container.BindSingleton(instance);
    }
    
    protected void RegisterSingleton<TInterface, TConcrete>() where TConcrete : TInterface
    {
        Container.BindSingleton<TInterface, TConcrete>();
    }
    
    protected void RegisterEntity<T>() where T : IEntity
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