using System.Collections;

namespace NodeGraphApp;

public sealed class FlexColumn : IEnumerable<FlexItem>
{
    public ScreenRect Bounds { get; set; }
    public Padding Padding { get; set; }
    public float ItemGap { get; set; }
    
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
        var totalFlexSpace = Bounds.Height - Padding.Top - Padding.Bottom;

        foreach (var item in _items)
        {
            totalFlexGrow += item.FlexGrow;
            if (item.FlexGrow == 0f)
                totalFlexSpace -= item.BaseHeight;
        }
        
        totalFlexSpace -= ItemGap * (_items.Count - 1);

        var top = Bounds.Top - Padding.Top;
        var left = Bounds.Left + Padding.Left;
        var width = Bounds.Width - Padding.Left - Padding.Right;
        foreach (var item in _items)
        {
            var height = item.BaseHeight;
            if (item.FlexGrow > 0)
            {
                var flexShare = item.FlexGrow / totalFlexGrow;
                height = flexShare * totalFlexSpace;
            }

            item.Bounds = ScreenRect.FromLeftTopWidthHeight(left, top, width, height);
            top -= height + ItemGap;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}