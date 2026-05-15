namespace ZGF.Gui;

public interface IViewBehavior
{
    void AttachToContext(MultiChildView view, Context context);
    void DetachFromContext(MultiChildView view, Context context);
}
