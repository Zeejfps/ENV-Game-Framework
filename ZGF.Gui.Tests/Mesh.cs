using OpenGL.NET;
using ZGF.WavefrontObjModule;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;
using static OpenGL.NET.GLBuffer;

namespace ZGF.Gui.Tests;

public sealed class Mesh
{
    public uint VaoId { get; }
    public uint VboId { get; }
    public uint IboId { get; }

    public static unsafe Mesh LoadFromFile(string pathToMeshFile)
    {
        var objFileContents = WavefrontObj.ReadFromFile(pathToMeshFile);

        uint vao;
        glGenVertexArrays(1, &vao);
        AssertNoGlError();

        glBindVertexArray(vao);
        AssertNoGlError();

        uint vertexPositionBufferId, indexBufferId;
        glGenBuffers(1, &vertexPositionBufferId);
        AssertNoGlError();

        var vertexPositionBuffer = glBindBuffer<VertexPosition>(GL_ARRAY_BUFFER, vertexPositionBufferId);
        AssertNoGlError();

        glBufferData(vertexPositionBuffer, objFileContents.VertexPositions.Span, BufferUsageHint.StaticDraw);
        AssertNoGlError();

        glVertexAttribPointer<float>(0);
        AssertNoGlError();

        glEnableVertexAttribArray(0);
        AssertNoGlError();

        glGenBuffers(1, &indexBufferId);
        AssertNoGlError();

        glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, indexBufferId);

        return null;
    }
    
}