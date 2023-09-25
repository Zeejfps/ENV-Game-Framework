using BmFont;

namespace OpenGLSandbox;

public sealed class BitmapFontRenderingScene : IScene
{
    public void Load()
    {
        var font = FontLoader.Load("Assets/bitmapfonts/test.fnt");
        Console.WriteLine(font.Chars[10].ID);
    }

    public void Render()
    {
    }

    public void Unload()
    {
    }
}