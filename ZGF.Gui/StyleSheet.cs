using System.Diagnostics.CodeAnalysis;

namespace ZGF.Gui;

public sealed class StyleSheet
{
    public bool TryGetById(string? id, [NotNullWhen(true)] out Style? style)
    {
        if (string.IsNullOrEmpty(id))
        {
            style = null;
            return false;
        }
        style = null;
        return false;
    }
    
    public bool TryGetByClass(string? classId, [NotNullWhen(true)] out Style? style)
    {
        if (string.IsNullOrEmpty(classId))
        {
            style = null;
            return false;
        }
        
        style = null;
        return false;
    }
}