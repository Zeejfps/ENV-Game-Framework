using System.Diagnostics.CodeAnalysis;

namespace ZGF.Gui;

public sealed class StyleSheet
{
    private readonly Dictionary<string, Style> _styleByIdLookup = new();
    private readonly Dictionary<string, Style> _styleByClassLookup = new();
    
    public void AddStyleForId(string id, Style style)
    {
        _styleByIdLookup[id] = style;
    }

    public void RemoveStyleForId(string id)
    {
        _styleByIdLookup.Remove(id);
    }
    
    public void AddStyleForClass(string classId, Style style)
    {
        _styleByClassLookup[classId] = style;
    }
    
    public void RemoveStyleForClass(string classId)
    {
        _styleByClassLookup.Remove(classId);
    }
    
    public bool TryGetById(string? id, [NotNullWhen(true)] out Style? style)
    {
        if (string.IsNullOrEmpty(id))
        {
            style = null;
            return false;
        }
        
        return _styleByIdLookup.TryGetValue(id, out style);
    }
    
    public bool TryGetByClass(string? classId, [NotNullWhen(true)] out Style? style)
    {
        if (string.IsNullOrEmpty(classId))
        {
            style = null;
            return false;
        }
        
        return _styleByClassLookup.TryGetValue(classId, out style);
    }
}