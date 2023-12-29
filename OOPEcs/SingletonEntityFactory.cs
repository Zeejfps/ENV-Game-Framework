using EasyGameFramework.Api;

namespace Tetris;

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