using Framework.Assets;
using Vortice.ShaderCompiler;

namespace AssetImporter;

public class ImportMaterialOption
{
    public void Run()
    {
        var options = new Options();
        options.SetSourceLanguage(SourceLanguage.GLSL);
        options.SetTargetEnv(TargetEnvironment.OpenGL, 450);
        using var compiler = new Compiler(options);

        Console.WriteLine("[Import Material]");
        var vertexShaderPath = ReadPath("Vertex shader path: ");

        var vertexShaderSource = File.ReadAllText(vertexShaderPath);
        var vertexShaderFileName = Path.GetFileName(vertexShaderPath);

        using var vertexShaderCompilationResult = compiler.Compile(vertexShaderSource, vertexShaderFileName, ShaderKind.VertexShader);
        if (vertexShaderCompilationResult.Status != CompilationStatus.Success)
        {
            Console.WriteLine(vertexShaderCompilationResult.ErrorMessage);
            return;
        }
        
        var fragmentShaderPath = ReadPath("Fragment shader path: ");

        var fragmentShaderSource = File.ReadAllText(fragmentShaderPath);
        var fragmentShaderFileName = Path.GetFileName(fragmentShaderPath);
        
        using var fragmentShaderCompilationResult = compiler.Compile(fragmentShaderSource, fragmentShaderFileName, ShaderKind.FragmentShader);
        if (fragmentShaderCompilationResult.Status != CompilationStatus.Success)
        {
            Console.WriteLine(fragmentShaderCompilationResult.ErrorMessage);
            return;
        }
        
        var materialAsset = new MaterialAsset
        {
            VertexShader = vertexShaderCompilationResult.GetBytecode().ToArray(),
            FragmentShader = fragmentShaderCompilationResult.GetBytecode().ToArray(),
        };
        
        Console.Write("Save As: ");
        var outputPath = Console.ReadLine();
        if (string.IsNullOrEmpty(outputPath))
        {
            Console.WriteLine("Error invalid path");
            return;
        }

        outputPath = outputPath.Replace("\"", "");

        using var stream = File.Open(Path.GetFullPath(outputPath), FileMode.OpenOrCreate);
        using var writer = new BinaryWriter(stream);
        materialAsset.Serialize(writer);
        
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