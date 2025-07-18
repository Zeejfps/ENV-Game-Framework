namespace ZGF.Gui;

public sealed class Context
{
    public required MouseInputSystem MouseInputSystem { get; init; }
    public required ITextMeasurer TextMeasurer { get; init; }
    public required IAssetManager AssetManager { get; init; }

    private readonly Dictionary<Type, object> _services = new();

    public void AddService<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }
    
    public T? Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
            return service as T;
        return null;
    }
}

public interface ITextMeasurer
{
    float MeasureTextWidth(string text, TextStyle style);
    float MeasureTextHeight(string text, TextStyle style);
}