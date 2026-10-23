namespace SampleApp;

public class Counter
{
    public int Count { get; private set; }

    public event Action? Changed;

    public void Increment()
    {
        Count++;
        Changed?.Invoke();
    }

    public void Reset()
    {
        Count = 0;
        Changed?.Invoke();
    }
}
