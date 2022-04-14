// See https://aka.ms/new-console-template for more information

using GlfwOpenGLBackend;
using SnakeGame;

using var context = new Context_GLFW();
var game = new Game(context);
game.Run();