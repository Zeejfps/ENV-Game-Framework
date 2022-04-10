using System.Numerics;

namespace ENV.Engine;

public interface IRenderPass
{
    void Execute(ICommandBuffer commandBuffer, IEnumerable<IRenderable> renderables);
}

public interface ICommandBuffer
{
    void SubmitClearCommand();
    void SubmitDrawCommand(IMesh mesh, IMaterial material);
}

public interface IRenderable
{
    IMesh Mesh { get; }
    IMaterial Material { get; }
    Matrix4x4 Transform { get; }
}

public interface IRenderer
{
    void Add(IRenderPass renderPass);
    void Remove(IRenderPass renderPass);
    void Clear();
    void RenderMesh(IMesh mesh, IMaterial material);
    void Render();
}