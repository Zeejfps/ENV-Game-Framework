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

    uint textureId;
    glGenTextures(1, &textureId);
    AssertNoGlError();
    
    glBindTexture(GL_TEXTURE_2D, textureId);
    AssertNoGlError();
    
    glActiveTexture(GL_TEXTURE0);
    AssertNoGlError();
    
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_NEAREST);
    AssertNoGlError();

    var colorBuffer = new Bitmap(640, 480);
    
    Graphics.FillRect(colorBuffer, 0, 0, 100, 150, 0xFF00FF);

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
    glUseProgram(shaderProgram.Id);
    
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
    
    uint VBO, VAO, EBO;
    glGenVertexArrays(1, &VAO);
    glGenBuffers(1, &VBO);
    glGenBuffers(1, &EBO);

    glBindVertexArray(VAO);

    glBindBuffer(GL_ARRAY_BUFFER, VBO);
    fixed (void* v = &vertices[0])
    {
        glBufferData(GL_ARRAY_BUFFER, vertices.Length * sizeof(float), v, GL_STATIC_DRAW);
    }

    glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, EBO);
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
        glBindVertexArray(VAO);
        glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, (void*)0);
            
        Glfw.SwapBuffers(windowHandle);
    }
    
    glDeleteVertexArrays(1, &VAO);
    glDeleteBuffers(1, &VBO);
    glDeleteBuffers(1, &EBO);
    glDeleteProgram(shaderProgram.Id);
    glDeleteTextures(1, &textureId);
    Glfw.Terminate();
}