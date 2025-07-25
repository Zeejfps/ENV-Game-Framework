namespace ZGF.WavefrontObjModule;

public static class WavefrontObj
{
    public static IWavefrontObjFileContents ReadFromFile(string pathToFile)
    {
        var reader = new WavefrontObjFileReader();
        return reader.ReadFromFile(pathToFile);
    }
}
