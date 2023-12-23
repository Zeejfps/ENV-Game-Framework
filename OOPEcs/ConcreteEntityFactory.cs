using EasyGameFramework.Api;

namespace Tetris;

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