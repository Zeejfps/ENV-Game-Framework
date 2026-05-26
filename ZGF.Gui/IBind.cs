namespace ZGF.Gui;

/// <summary>
/// Marks a view as bindable to <typeparamref name="TVm"/>. Pairs with the
/// <c>UseViewModel&lt;TVm&gt;(IBind&lt;TVm&gt;)</c> extension to formalize the data
/// contract between a view and its view model: self-bound when the implementing view
/// passes <c>this</c>, parent-owned when a parent passes the child as the target.
/// </summary>
public interface IBind<in TVm> where TVm : class
{
    void Bind(TVm vm);
}
