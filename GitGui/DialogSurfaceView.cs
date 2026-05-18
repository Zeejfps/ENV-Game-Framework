using ZGF.Gui;
using ZGF.Gui.Views;

namespace GitGui;

public sealed class DialogSurfaceView : MultiChildView
{
    private readonly MultiChildView _overlay;
    
    public DialogSurfaceView()
    {
        _overlay = new RectView
        {
            BackgroundColor = 0xB0000000,
            ZIndex = 1000,
        };
    }
    
    public void ShowDialog(View dialog)
    {
        Children.Add(_overlay);
        _overlay.Children.Add(new CenterView
        {
            Children =
            {
                dialog,
            }
        });
    }

    public void HideDialog()
    {
        _overlay.Children.Clear();
        Children.Remove(_overlay);
    }
}