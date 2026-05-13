namespace ZGF.Gui;

public interface IViewBehavior
{
    void AttachToContext(View view, Context context);
    void DetachFromContext(View view, Context context);
}
