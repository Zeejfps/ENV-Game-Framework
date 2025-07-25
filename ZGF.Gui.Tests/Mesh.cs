using ZGF.WavefrontObjModule;

namespace ZGF.Gui.Tests;

public sealed class Mesh
{
    public uint VaoId { get; }
    public uint VboId { get; }
    public uint IboId { get; }

    public static Mesh LoadFromFile(string pathToMeshFile)
    {
        var objFileContents = WavefrontObj.ReadFromFile(pathToMeshFile); 
        
        return null;
    }
    
}