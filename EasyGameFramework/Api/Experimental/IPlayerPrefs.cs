namespace EasyGameFramework.Api;

public interface IPlayerPrefs
{
    Task<T> LoadInputBindingsAsync<T>(CancellationToken cancellationToke = default) where T : IInputBindings;

    Task SaveInputBindingsAsync(IInputBindings inputBindings, CancellationToken cancellationToke = default);
}