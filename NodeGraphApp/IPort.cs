namespace NodeGraphApp;

public interface IPort
{
    bool IsHovered { get; set; }
    Node? Node { get;  }
}