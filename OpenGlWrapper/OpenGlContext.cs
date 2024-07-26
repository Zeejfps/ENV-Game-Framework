using static GL46;
using static OpenGlWrapper.OpenGlUtils;

namespace OpenGlWrapper;

public sealed class OpenGlContext
{
    public ArrayBufferManager ArrayBufferManager { get; }
    public VertexArrayObjectManager VertexArrayObjectManager { get; }

    private OpenGlContext()
    {
        ArrayBufferManager = new ArrayBufferManager();
        VertexArrayObjectManager = new VertexArrayObjectManager(ArrayBufferManager);
    }

    public static OpenGlContext Init(GetProcAddressDelegate getProcAddressDelegate)
    {
        Import(getProcAddressDelegate);
        return new OpenGlContext();
    }

    public void SetClearColor(float r, float g, float b, float a)
    {
        glClearColor(r, g, b, a);
    }

    public void Clear(ClearFlags flags)
    {
        glClear((uint)flags);
    }

    public void DrawArrayOfTriangles(VertexArrayObjectHandle vao, int count)
    {
        VertexArrayObjectManager.Bind(vao);
        glDrawArrays(GL_TRIANGLES, 0, count);
        AssertNoGlError();
    }
}