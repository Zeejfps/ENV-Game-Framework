namespace ZGF.WavefrontObjModule;

internal sealed class WavefrontObjFileReader
{
    private readonly CommentReader _commentReader = new();
    private readonly MaterialReader _materialReader = new();
    private readonly VertexReader _vertexReader = new();
    private readonly ObjectNameReader _objectNameReader = new();
    private readonly SmoothGroupReader _smoothGroupReader = new();
    private readonly List<SomethingObject> _objects = new();
    
    private readonly List<VertexPosition> _vertexPositions = new();
    private readonly List<VertexNormal> _vertexNormals = new();
    private readonly List<VertexTextureCoord> _vertexTextureCoords = new();
    private readonly List<Face> _faces = new();

    private SomethingObject? _currentObject;
    private int _vertexPositionIndex = 0;
    private int _vertexNormalsIndex = 0;
    private int _vertexTextureCoordsIndex = 0;
    private int _facesIndex = 0;

    public IWavefrontObjFileContents ReadFromFile(string pathToFile)
    {
        _vertexPositions.Clear();
        _vertexNormals.Clear();
        _vertexTextureCoords.Clear();
        _faces.Clear();

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
                        ReadNamedObjectData(textReader);
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
                        ReadFaceData(textReader);
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

        SetObjectData();

        var vertexPositions = _vertexPositions.ToArray();
        var vertexNormals = _vertexNormals.ToArray();
        var vertexTextureCoords = _vertexTextureCoords.ToArray();
        var faces = _faces.ToArray();

        var namedObjects = _objects
            .Select(t => new NamedObject
            {
                Name = t.Name,
                VertexPositions = vertexPositions.AsMemory(t.VertexPositions),
                VertexNormals = new ReadOnlyMemory<VertexNormal>(
                    vertexNormals,
                    t.VertexNormalsIndex,
                    t.VertexNormalsCount
                ),
                VertexTextureCoords = new ReadOnlyMemory<VertexTextureCoord>(
                    vertexTextureCoords,
                    t.VertexTextureCoordsIndex,
                    t.VertexTextureCoordsCount
                ),
                Faces = new ReadOnlyMemory<Face>(
                    faces,
                    t.FacesIndex,
                    t.FacesCount
                ),
            })
            .ToArray();


        var data = new SomethingContent
        {
            VertexPositions = _vertexPositions.ToArray(),
            VertexNormals = _vertexNormals.ToArray(),
            VertexTextureCoords = _vertexTextureCoords.ToArray(),
            Faces = _faces.ToArray()
        };
        
        return new WavefrontObjFileContents(data);;
    }

    private void ReadFaceData(StreamReader textReader)
    {
        var buffer = new char[64];
        int charAsInt;
        var len = 0;
        
        Span<int> indices = stackalloc int[3];
        Span<Triangle> trianglesBuffer = stackalloc Triangle[4];
        var triangleCount = 0;
        var indexCount = 0;
        while ((charAsInt = textReader.Read()) > 0)
        {
            if (charAsInt == '/')
            {
                indices[indexCount] = int.Parse(buffer.AsSpan(0, len));
                indexCount++;
                len = 0;
                continue;
            }
            
            if (charAsInt == ' ')
            {
                indices[indexCount] = int.Parse(buffer.AsSpan(0, len));
                trianglesBuffer[triangleCount] = new Triangle
                {
                    V0 = indices[0],
                    V1 = indices[1],
                    V2 = indices[2]
                };
                triangleCount++;
                
                len = 0;
                indexCount = 0;

                continue;
            }
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n') break;
            
            buffer[len] = (char)charAsInt;
            len++;
        }

        var face = new Face
        {
            Triangles = trianglesBuffer
                .Slice(0, triangleCount)
                .ToArray()
        };
        
        _faces.Add(face);
    }

    private void SetObjectData()
    {
        if (_currentObject != null)
        {
            _currentObject.VertexPositions = new Range(_vertexPositionIndex, _vertexPositions.Count);
            _vertexPositionIndex += _vertexPositions.Count;

            _currentObject.SetVertexTextureCoordsRange(_vertexTextureCoordsIndex, _vertexTextureCoords.Count - _vertexTextureCoordsIndex);
            _vertexTextureCoordsIndex += _vertexTextureCoords.Count;

            _currentObject.SetVertexNormalsRange(_vertexNormalsIndex, _vertexNormals.Count - _vertexNormalsIndex);
            _vertexNormalsIndex += _vertexNormals.Count;

            _currentObject.SetFacesRange(_facesIndex, _faces.Count - _facesIndex);
            _facesIndex += _faces.Count;
        }
    }
    
    private void ReadNamedObjectData(StreamReader textReader)
    {
        SetObjectData();
        
        var objName = _objectNameReader.Read(textReader);
        var namedObject = new SomethingObject()
        {
            Name = new string(objName),
        };
        _objects.Add(namedObject);
    }
}