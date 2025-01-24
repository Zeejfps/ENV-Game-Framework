using System.Collections;
using EasyGameFramework.GUI;

namespace Bricks.RaylibBackend;

public interface IRenderCommand
{
    void Render();
}

public sealed class CommandBuffer
{
    private LinkedList<IRenderCommand> _commands = new();
    
    public void Add(IRenderCommand command)
    {
        _commands.AddLast(command);
    }

    public void Remove(IRenderCommand command)
    {
        _commands.Remove(command);
    }

    public void Clear()
    {
        _commands.Clear();
    }
    
    public IEnumerable<IRenderCommand> Commands => _commands;
}

public sealed class RaylibGuiContext : IBuildContext
{
    public IPanelRenderer PanelRenderer => _raylibPanelRenderer;
    public ITextRenderer TextRenderer => _raylibTextRenderer;
    public FocusTree FocusTree { get; }

    private readonly RaylibTextRenderer _raylibTextRenderer;
    private readonly RaylibPanelRenderer _raylibPanelRenderer;
    
    private readonly CommandBuffer _commandBuffer = new();
    
    public RaylibGuiContext()
    {
        _raylibTextRenderer = new RaylibTextRenderer(_commandBuffer);
        _raylibPanelRenderer = new RaylibPanelRenderer(_commandBuffer);
    }

    public void Render()
    {
        foreach (var renderCommand in _commandBuffer.Commands)
            renderCommand.Render();
    }
}