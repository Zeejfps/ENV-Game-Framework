using System.Collections;

namespace NodeGraphApp;

public sealed class FlexColumn : IEnumerable<FlexItem>
{
    public ScreenRect Bounds { get; set; }
    public Padding Padding { get; set; }
    
    private readonly List<FlexItem> _items = [];
    
    public IEnumerator<FlexItem> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    public void Add(FlexItem flexItem)
    {
        _items.Add(flexItem);
    }

    public void DoLayout()
    {
        var totalFlexGrow = 0f;
        var totalFlexSpace = Bounds.Height;

        foreach (var item in _items)
        {
            totalFlexGrow += item.FlexGrow;
            if (item.FlexGrow == 0f)
                totalFlexSpace -= item.BaseHeight;
        }

        var top = Bounds.Top;
        var left = Bounds.Left;
        var width = Bounds.Width;
        foreach (var item in _items)
        {
            var height = item.BaseHeight;
            if (item.FlexGrow > 0)
            {
                var flexShare = item.FlexGrow / totalFlexGrow;
                height = flexShare * totalFlexSpace;
            }

            item.Bounds = ScreenRect.FromLTWH(left, top, width, height);
            top -= height;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}