using System.Collections;

namespace NodeGraphApp;

public sealed class FlexColumn : IEnumerable<FlexItem>
{
    public IEnumerator<FlexItem> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public void Add(FlexItem flexItem)
    {
        throw new NotImplementedException();
    }

    public void Update()
    {

    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public sealed class FlexItem
{
    public float FlexGrow { get; set; }
    public float BaseHeight { get; set; }
    public Action<float, float, float, float>? OnLayout { get; set; }

    public void Update()
    {

    }
}