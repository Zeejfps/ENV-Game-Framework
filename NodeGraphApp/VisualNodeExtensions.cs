using System.Diagnostics.CodeAnalysis;

namespace NodeGraphApp;

public static class VisualNodeExtensions
{
    public static bool ChildOf<T>(this VisualNode node, [NotNullWhen(true)] out T? parentOfType) where T : VisualNode
    {
        var parent = node.Parent;
        while (parent != null)
        {
            if (parent is T childOfType)
            {
                parentOfType = childOfType;
                return true;
            }
            parent = parent.Parent;
        }
        parentOfType = null;
        return false;
    }
}