using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class BorderLayout : Component
{
    public Component? North { get; set; }
    public Component? East { get; set; }
    public Component? West { get; set; }
    public Component? South { get; set; }
    public Component? Center { get; set; }

    protected override void OnLayoutSelf()
    {
        Position = Constraints;
        var position = Position;
        
        var centerAreaWidth = position.Width;
        var centerAreaHeight = position.Height;

        var leftOffset = 0f;
        var bottomOffset = 0f;
        
        if (North != null)
        {
            North.Constraints = new RectF(position.Left, position.Top - North.Constraints.Height, position.Width, North.Constraints.Height);
            North.LayoutSelf();
            centerAreaHeight -= North.Position.Height;
        }

        if (South != null)
        {
            South.Constraints = new RectF(
                position.Left,
                position.Bottom,
                position.Width,
                South.Constraints.Height);
            
            South.LayoutSelf();
            centerAreaHeight -= South.Position.Height;
            bottomOffset += South.Position.Height;
        }

        if (West != null)
        {
            West.Constraints = new RectF
            {
                Left = position.Left,
                Bottom = position.Bottom + bottomOffset,
                Width = West.Constraints.Width,
                Height = centerAreaHeight,
            };
            West.LayoutSelf();
            centerAreaWidth -= West.Position.Width;
            leftOffset += West.Position.Width;
        }
        
        if (East != null)
        {
            East.Constraints = new RectF
            {
                Left = position.Right - East.Constraints.Width,
                Bottom = position.Bottom + bottomOffset,
                Width = East.Constraints.Width,
                Height = centerAreaHeight,
            };
            East.LayoutSelf();
            centerAreaWidth -= East.Position.Width;
        }

        if (Center != null)
        {
            Center.Constraints = new RectF
            {
                Left = position.Left + leftOffset,
                Width = centerAreaWidth,
                Bottom = position.Bottom + bottomOffset,
                Height = centerAreaHeight
            };
            Center.LayoutSelf();
        }
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        North?.DrawSelf(c);
        Center?.DrawSelf(c);
        South?.DrawSelf(c);
        West?.DrawSelf(c);
        East?.DrawSelf(c);
    }

    public override bool IsDirty
    {
        get
        {
            if (North != null && North.IsDirty)
                return true;

            if (South != null && South.IsDirty)
                return true;

            if (Center != null && Center.IsDirty)
                return true;

            if (West != null && West.IsDirty)
                return true;
            
            if (East != null && East.IsDirty)
                return true;

            return base.IsDirty;
        }
    }
}