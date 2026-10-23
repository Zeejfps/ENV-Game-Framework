namespace ZGF.ILSpy.App.Tests.Fixtures;

public class Counter
{
    public int Count { get; set; }

    public event Action? Changed;

    public void Increment()
    {
        Count++;
        Changed?.Invoke();
    }

    public void Watch(Action handler) => Changed += handler;
}
