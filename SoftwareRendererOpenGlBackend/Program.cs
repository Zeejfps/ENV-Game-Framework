using GLFW;
using OpenGL.NET;
using SoftwareRendererModule;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;
using static OpenGL.NET.GLBuffer;
using static OpenGL.NET.GLTexture;
using Monitor = GLFW.Monitor;

unsafe
{
    Glfw.Init();

    Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
    Glfw.WindowHint(Hint.ContextVersionMajor, 4);
    Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);

    var windowWidth = 640;
    var windowHeight = 480;
    var windowHandle = Glfw.CreateWindow(windowWidth, windowHeight, "Software Renderer", Monitor.None, Window.None);
    
    Glfw.MakeContextCurrent(windowHandle);
    Glfw.ShowWindow(windowHandle);
    Glfw.SwapInterval(1);

    Import(Glfw.GetProcAddress);
    AssertNoGlError();
    
    var colorBuffer = new Bitmap(320, 240);

    var texture = new Texture2DBuilder()
        .WithMinFilter(TextureMinFilter.Nearest)
        .WithMagFilter(TextureMagFilter.Nearest)
        .BindAndBuild();
    AssertNoGlError();
    
    glTexImage2D<uint>(texture, 0,  GL_RGBA8, colorBuffer.Width, colorBuffer.Height, 
        GL_RGBA, GL_UNSIGNED_BYTE, colorBuffer.Pixels);
    AssertNoGlError();

    var shaderProgram = new ShaderProgramCompiler()
        .WithVertexShader("Assets/tex.vert.glsl")
        .WithFragmentShader("Assets/tex.frag.glsl")
        .Compile();
    
    float[] vertices =
    {
        // Positions        // Texture Coords
        1.0f,  1.0f, 0.0f,  1.0f, 1.0f, // top right
        1.0f, -1.0f, 0.0f,  1.0f, 0.0f, // bottom right
        -1.0f, -1.0f, 0.0f,  0.0f, 0.0f, // bottom left
        -1.0f,  1.0f, 0.0f,  0.0f, 1.0f  // top left
    };

    uint[] indices =
    {
        0, 1, 3, // first triangle
        1, 2, 3  // second triangle
    };
    
    uint vbo, vao, ibo;
    glGenVertexArrays(1, &vao);
    glGenBuffers(1, &vbo);
    glGenBuffers(1, &ibo);

    glBindVertexArray(vao);

    var vertexDataBuffer = glBindBuffer<float>(GL_ARRAY_BUFFER, vbo);
    glBufferData(vertexDataBuffer, vertices, BufferUsageHint.StaticDraw);
    AssertNoGlError();

    glVertexAttribPointer<float>(
        attribIndex: 0,
        count: 3,
        stride: 5,
        offset: 0
    );
    AssertNoGlError();

    glVertexAttribPointer<float>(
        attribIndex: 1,
        count: 2,
        stride: 5,
        offset: 3
    );
    AssertNoGlError();

    glEnableVertexAttribArray(0);
    glEnableVertexAttribArray(1);

    var indexDataBuffer = glBindBuffer<uint>(GL_ELEMENT_ARRAY_BUFFER, ibo);
    glBufferData(indexDataBuffer, indices, BufferUsageHint.StaticDraw);
    
    SizeCallback windowSizeCallback = (window, width, height) =>
    {
        glViewport(0, 0, width, height);
        glClear(GL_COLOR_BUFFER_BIT);
        Render();
        Glfw.SwapBuffers(window);
    };
    
    Glfw.SetWindowSizeCallback(windowHandle, windowSizeCallback);
    glClearColor(0.2f, 0.3f, 0.3f, 1.0f);

    while (!Glfw.WindowShouldClose(windowHandle))
    {
        Glfw.PollEvents();
        
        Render();

        Glfw.SwapBuffers(windowHandle);
    }
    
    glDeleteVertexArrays(1, &vao);
    glDeleteBuffers(1, &vbo);
    glDeleteBuffers(1, &ibo);
    glDeleteProgram(shaderProgram.Id);

    var textureId = texture.Id;
    glDeleteTextures(1, &textureId);
    Glfw.Terminate();

    var myVao = vao;

    void Render()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        colorBuffer.Fill(0x000000);
        
        Graphics.DrawRect(colorBuffer, 300, 200, 100, 150, 0xFF00FF);
        Graphics.FillRect(colorBuffer, 0, 0, 100, 150, 0xFF00FF);
        Graphics.DrawLine(colorBuffer, 0, 200, 100, 200, 0xFF00FF);
        Graphics.DrawLine(colorBuffer, 50, 200, 50, 300, 0xFF00FF);
        Graphics.DrawLine(colorBuffer, 100, 200, 150, 300, 0xFF00FF);
        
        fixed(void* pixelDataPtr = &colorBuffer.Pixels[0])
            glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, colorBuffer.Width, colorBuffer.Height, GL_RGBA, GL_UNSIGNED_BYTE, pixelDataPtr);
        
        // Drawing the textured quad.
        glUseProgram(shaderProgram.Id);
        glBindVertexArray(myVao);
        glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, (void*)0);
    }
}