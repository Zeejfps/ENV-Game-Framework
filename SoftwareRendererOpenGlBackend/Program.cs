using GLFW;
using OpenGL.NET;
using SoftwareRendererModule;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;
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

    var textureId = new Texture2DBuilder()
        .WithMinFilter(TextureMinFilter.Nearest)
        .WithMagFilter(TextureMagFilter.Nearest)
        .Build();

    var colorBuffer = new Bitmap(640, 480);
    
    Graphics.FillRect(colorBuffer, 0, 0, 100, 150, 0xFF00FF);
    
    Graphics.DrawLineH(colorBuffer, 0, 200, 100, 0xFF00FF);
    Graphics.DrawLineV(colorBuffer, 50, 200, 100, 0xFF00FF);
    Graphics.DrawLine(colorBuffer, 100, 200, 150, 300, 0xFF00FF);

    fixed (void* ptr = &colorBuffer.Pixels[0])
    {
        glTexImage2D(GL_TEXTURE_2D, 0, (int)GL_RGBA8, 
            colorBuffer.Width, colorBuffer.Height, 0, GL_RGBA, GL_UNSIGNED_BYTE, ptr);
        AssertNoGlError();
    }

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
    
    uint vbo, vao, ebo;
    glGenVertexArrays(1, &vao);
    glGenBuffers(1, &vbo);
    glGenBuffers(1, &ebo);

    glBindVertexArray(vao);

    glBindBuffer(GL_ARRAY_BUFFER, vbo);
    fixed (void* v = &vertices[0])
    {
        glBufferData(GL_ARRAY_BUFFER, vertices.Length * sizeof(float), v, GL_STATIC_DRAW);
    }

    glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, ebo);
    fixed (void* i = &indices[0])
    {
        glBufferData(GL_ELEMENT_ARRAY_BUFFER, indices.Length * sizeof(uint), i, GL_STATIC_DRAW);
    }

    // Position attribute.
    glVertexAttribPointer(0, 3, GL_FLOAT, false, 5 * sizeof(float), (void*)0);
    glEnableVertexAttribArray(0);

    // Texture coord attribute. [9]
    glVertexAttribPointer(1, 2, GL_FLOAT, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));
    glEnableVertexAttribArray(1);
    
    while (!Glfw.WindowShouldClose(windowHandle))
    {
        Glfw.PollEvents();
    
        glClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        glClear(GL_COLOR_BUFFER_BIT);

        // Drawing the textured quad.
        glUseProgram(shaderProgram.Id);
        glBindVertexArray(vao);
        glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, (void*)0);
            
        Glfw.SwapBuffers(windowHandle);
    }
    
    glDeleteVertexArrays(1, &vao);
    glDeleteBuffers(1, &vbo);
    glDeleteBuffers(1, &ebo);
    glDeleteProgram(shaderProgram.Id);
    glDeleteTextures(1, &textureId);
    Glfw.Terminate();
}