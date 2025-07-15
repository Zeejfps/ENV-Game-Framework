using ZGF.Geometry;

namespace ZGF.Gui;

public class BorderLayout : ILayout
{
    public Component? North { get; set; }
    public Component? East { get; set; }
    public Component? West { get; set; }
    public Component? South { get; set; }
    public Component? Center { get; set; }

    public RectF DoLayout(RectF position)
    {
        var centerAreaHeight = position.Height;
        var bottomOffset = 0f;
        if (North != null)
        {
            North.Position = new RectF(position.Left, position.Top - North.Position.Height, position.Width, North.Position.Height);
            North.LayoutSelf();
            centerAreaHeight -= North.Position.Height;
        }

        if (South != null)
        {
            South.LayoutSelf();
            South.Position = new RectF(position.Left, position.Bottom, position.Width, South.Position.Height);
            South.LayoutSelf();
            centerAreaHeight -= South.Position.Height;
            bottomOffset += South.Position.Height;
        }

        if (Center != null)
        {
            Console.WriteLine("Wtf");
            Center.Position = new RectF(position.Left, position.Bottom + bottomOffset, position.Width, centerAreaHeight);
            Center.LayoutSelf();
        }

        return position;
    }

    public void ApplyStyleSheet(StyleSheet styleSheet)
    {

    }

    public void DrawSelf(ICanvas canvas)
    {
        North?.DrawSelf(canvas);
        Center?.DrawSelf(canvas);
        South?.DrawSelf(canvas);
    }

    public bool IsDirty
    {
        get
        {
            if (North != null && North.IsDirty)
                return true;

            if (South != null && South.IsDirty)
                return true;

            if (Center != null && Center.IsDirty)
                return true;

            return false;
        }
    }
}