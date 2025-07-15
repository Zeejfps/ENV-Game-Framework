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
        var centerAreaHeight = position.Height;
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

        if (Center != null)
        {
            Center.Constraints = position with
            {
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

            return base.IsDirty;
        }
    }
}