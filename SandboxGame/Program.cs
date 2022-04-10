using Framework;
using Framework.GLFW.NET;

using (var context = new Context_GLFW())
{
    var game = new Game(context);
    game.Run();
}