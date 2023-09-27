namespace OpenGLSandbox;

class RectRenderPass
{

    public void Execute(ICommandBuffer commandBuffer)
    {
        var commands = commandBuffer.GetAll<DrawRectCommand>();
        foreach (var command in commands)
        {
            
        }
    }
    
}