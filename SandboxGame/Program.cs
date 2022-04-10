using ENV;
using ENV.GLFW.NET;

using (var context = new Context_GLFW())
{
    var game = new Game(context);
    game.Run();
}