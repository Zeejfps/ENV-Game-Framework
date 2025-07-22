namespace ZGF.Gui;

public interface IController
{
    void OnAttachedToContext(Context context);
    void OnDetachedFromContext(Context context);
}