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

        Span<char> buff = stackalloc char[7];
        var len = 0;
        int chasAsInt;
        while ((chasAsInt = textReader.Read()) > 0)
        {
            if (chasAsInt == ' ')
            {
                var header = buff[..len];
                switch (header)
                {
                    case "#":
                        _commentReader.Read(textReader);
                        break;
                    case "mtllib":
                        var materialFileName = _materialReader.Read(textReader);
                        Console.WriteLine($"Material file: {materialFileName}");
                        break;
                    case "o":
                        ReadObjectData(textReader);
                        break;
                    case "v":
                        var vertexPosition = _vertexReader.ReadPosition(textReader);
                        //Console.WriteLine($"v {vertexPosition.X}, {vertexPosition.Y}, {vertexPosition.Z}, {vertexPosition.W}");
                        _vertexPositions.Add(vertexPosition);
                        break;
                    case "vn":
                        var vertexNormal = _vertexReader.ReadNormal(textReader);
                        //Console.WriteLine($"vn {vertexNormal.X}, {vertexNormal.Y}, {vertexNormal.Z}");
                        _vertexNormals.Add(vertexNormal);
                        break;
                    case "vt":
                        var vertexTexture = _vertexReader.ReadTextureCoords(textReader);
                        _vertexTextureCoords.Add(vertexTexture);
                        break;
                    case "s":
                        _smoothGroupReader.Read(textReader);
                        break;
                    case "f":
                        break;
                    default:
                        throw new Exception($"Unexpected header '{header}' encountered while reading obj file");
                }

                len = 0;
                continue;
            }
            
            buff[len] = (char)chasAsInt;
            len++;
        }

        return null;
    }
    
    private void ReadObjectData(StreamReader textReader)
    {
        var objName = _objectNameReader.Read(textReader);
        var namedObject = new NamedObject
        {
            Name = new string(objName),
            VertexPositions = [],
            VertexNormals = [],
            VertexTextureCoords = []
        };
        _vertexPositions = namedObject.VertexPositions;
        _vertexNormals = namedObject.VertexNormals;
        _vertexTextureCoords = namedObject.VertexTextureCoords;
        _objects.Add(namedObject);
        Console.WriteLine($"Reading object: {objName}");
    }
}