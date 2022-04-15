using Framework;
using Framework.GLFW.NET;
using GlfwOpenGLBackend;

using (var context = new Context_GLFW_GL())
{
    var game = new Game(context);
    game.Run();
}