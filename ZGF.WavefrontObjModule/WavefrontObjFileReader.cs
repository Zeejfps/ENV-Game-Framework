using System.Diagnostics;

namespace ZGF.WavefrontObjModule;

public readonly struct VertexPosition
{
    public required float X { get; init; }
    public required float Y { get; init; }
    public required float Z { get; init; }
    public required float W { get; init; }
}

public readonly struct VertexNormal
{
    public required float X { get; init; }
    public required float Y { get; init; }
    public required float Z { get; init; }
}

public readonly struct VertexTextureCoord
{
    public required float U { get; init; }
    public required float V { get; init; }
}

internal sealed class WavefrontObjFileReader
{
    private readonly CommentReader _commentReader = new();
    private readonly MaterialReader _materialReader = new();
    private readonly VertexReader _vertexReader = new();
    private readonly ObjectNameReader _objectNameReader = new();
    private readonly SmoothGroupReader _smoothGroupReader = new();
    private readonly List<NamedObject> _objects = new();
    
    private List<VertexPosition>? _vertexPositions;
    private List<VertexNormal>? _vertexNormals;
    private List<VertexTextureCoord>? _vertexTextureCoords;
    
    public IWavefrontObjFileContents ReadFromFile(string pathToFile)
    {
        using var fileStream = File.OpenRead(pathToFile);
        using var textReader = new StreamReader(fileStream);

        int chasAsInt;
        while ((chasAsInt = textReader.Peek()) > 0)
        {
            switch (chasAsInt)
            {
                case '#':
                    _commentReader.Read(textReader);
                    break;
                case 'm':
                    ReadMaterialData(textReader);
                    break;
                case 'o':
                    ReadObjectData(textReader);
                    break;
                case 'v':
                    ReadVertexData(textReader);
                    break;
                case 's':
                    ReadSmoothGroupData(textReader);
                    break;
                case 'f':
                    ReadFaceData(textReader);
                    break;
                default:
                    throw new Exception($"Unexpected character '{(char)chasAsInt}' encountered while reading obj file");
            }
        }

        return null;
    }

    private void ReadFaceData(StreamReader textReader)
    {
        
    }

    private void ReadSmoothGroupData(StreamReader textReader)
    {
        _smoothGroupReader.Read(textReader);
        Console.WriteLine("Read smoothing group");
    }

    private void ReadMaterialData(StreamReader textReader)
    {
        var materialFileName = _materialReader.Read(textReader);
        Console.WriteLine($"Material file: {materialFileName}");
    }
    
    private void ReadObjectData(StreamReader textReader)
    {
        var objName = _objectNameReader.Read(textReader);
        var namedObject = new NamedObject
        {
            Name = new string(objName),
            VertexPositions = [],
            VertexNormals = []
        };
        _vertexPositions = namedObject.VertexPositions;
        _vertexNormals = namedObject.VertexNormals;
        _vertexTextureCoords = namedObject.VertexTextureCoords;
        _objects.Add(namedObject);
        Console.WriteLine($"Reading object: {objName}");
    }

    private void ReadVertexData(StreamReader textReader)
    {
        Console.WriteLine("Reading vertex...");
        var v = textReader.Read();
        Debug.Assert(v == 'v', $"Expected 'v', found '{(char)v}'");
        
        var nextChar = textReader.Read();
        Console.WriteLine($"Next char: {(char)nextChar}");
        switch (nextChar)
        {
            case ' ':
                var vertexPosition = _vertexReader.ReadPosition(textReader);
                Console.WriteLine($"{vertexPosition.X}, {vertexPosition.Y}, {vertexPosition.Z}, {vertexPosition.W}");
                _vertexPositions.Add(vertexPosition);
                break;
            case 'n':
                var vertexNormal = _vertexReader.ReadNormal(textReader);
                _vertexNormals.Add(vertexNormal);
                break;
            case 't':
                var vertexTexture = _vertexReader.ReadTextureCoords(textReader);
                _vertexTextureCoords.Add(vertexTexture);
                break;
            default:
                throw new Exception($"Unexpected character '{(char)nextChar}' encountered while reading obj file");   
        }
    }
}