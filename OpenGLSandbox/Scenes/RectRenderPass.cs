using static GL46;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;

public unsafe class RectRenderPass
{
    private uint m_PerInstanceBuffer;
    private const uint MaxRectCount = 512;
    
    public void Execute(ICommandBuffer commandBuffer)
    {
        // var commands = commandBuffer.GetAll<DrawPanelCommand>();
        //
        // glBindBuffer(GL_ARRAY_BUFFER, m_PerInstanceBuffer);
        // AssertNoGlError();
        // // NOTE(Zee): Orphan the old buffer
        // var bufferPtr = glMapBufferRange(GL_ARRAY_BUFFER, IntPtr.Zero, new IntPtr(commands.Length * sizeof(Panel)), GL_MAP_WRITE_BIT | GL_MAP_INVALIDATE_BUFFER_BIT | GL_MAP_UNSYNCHRONIZED_BIT);
        // AssertNoGlError();
        //
        // var buffer = new Span<Panel>(bufferPtr, commands.Length);
        // for (var i = 0; i < commands.Length; i++)
        // {
        //     var command = commands[i];
        //     buffer[i] = new Panel
        //     {
        //         Color = command.Color,
        //         BorderSize = command.BorderSize,
        //     };
        // }
        //
        // glUnmapBuffer(GL_ARRAY_BUFFER);
        // AssertNoGlError();
    }
    
}