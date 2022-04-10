using ENV.Engine;

namespace ENV;

public class ClearPass : IRenderPass
{
    public void Execute(ICommandBuffer commandBuffer, IEnumerable<IRenderable> renderables)
    {
        commandBuffer.SubmitClearCommand();
    }
}