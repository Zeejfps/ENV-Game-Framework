namespace EasyGameFramework.Api.Rendering;

public interface IPipelineController
{
    void Bind(IHandle<IPipeline> pipelineHandle);
    void AttachBuffer(uint attachmentIndex, IHandle<IBuffer> bufferHandle);
}