namespace ZGF.Gui;

public interface IViewBehavior
{
    void OnAttachedToContext(View view, Context context);
    void OnDetachedFromContext(View view, Context context);
}
