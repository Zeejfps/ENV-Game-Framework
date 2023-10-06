namespace OpenGLSandbox;

public abstract class StatefulWidget : Widget
{
    private bool m_IsDirty;
        
    public override void Update(IBuildContext context)
    {
        if (m_IsDirty)
        {
            m_IsDirty = false;
            Dispose();
        }
        base.Update(context);
    }

    protected bool SetField<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        m_IsDirty = true;
        return true;
    }
}