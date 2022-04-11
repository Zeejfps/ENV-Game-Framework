namespace AssetImporter;

public class ImportMaterialOption
{
    private readonly MaterialAssetImporter_GL m_Importer = new();
    
    public void Run()
    {
        Console.WriteLine("[Import Material]");
        var vertexShaderPath = ReadPath("Vertex shader path: ");
        var vertexShaderSource = File.ReadAllText(vertexShaderPath);

        var fragmentShaderPath = ReadPath("Fragment shader path: ");
        var fragmentShaderSource = File.ReadAllText(fragmentShaderPath);
        
        Console.Write("Save As: ");
        var outputPath = Console.ReadLine();
        if (string.IsNullOrEmpty(outputPath))
        {
            Console.WriteLine("Error invalid path");
            return;
        }
        
        outputPath = outputPath.Replace("\"", "");
        
        m_Importer.VertexShaderSource = vertexShaderSource;
        m_Importer.FragmentShaderSource = fragmentShaderSource;
        m_Importer.Import(outputPath);
        
        Console.WriteLine($"Saved Material to: {outputPath}");
    }

    private string ReadPath(string message)
    {
        var validPathEntered = false;
        var path = string.Empty;

        while (!validPathEntered)
        {
            Console.Write(message);
            path = Console.ReadLine();
            if (string.IsNullOrEmpty(path))
            {
                OnInvalidPathEntered();
                continue;
            }
            
            path = path.Replace("\"", "");
            if (File.Exists(path))
                validPathEntered = true;
            else
                OnInvalidPathEntered();
        }

        return path;
    }
    
    private void OnInvalidPathEntered()
    {
        Console.WriteLine("Invalid path entered, please try again:");
    }
    
}