using Framework;
using GlfwOpenGLBackend;

using (var context = new Application_GLFW_GL())
{
    var game = new Game(context);
    game.Run();
}