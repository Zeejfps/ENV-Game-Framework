using static GL46;

namespace OpenGLSandbox;

public sealed class BitmapFontRenderingScene : IScene
{
    private TextRenderer TextRenderer { get; set; }
    
    public void Load()
    {
        TextRenderer = new TextRenderer();
        glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
        glClearColor(0f, 0.3f, 0.6f, 1f);
    }

    public void Render()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        var color = Color.FromHex(0xFF0045, 1f);
        TextRenderer.RenderText(
            new Rect(0, 0, 640f, 640f), 
            new TextStyle
            {
                VerticalTextAlignment = TextAlignment.Start,
                HorizontalTextAlignment = TextAlignment.Start,
                Color = Color.FromHex(0xff00ff, 1f),
            },
            "Top Left"
        );
        
        TextRenderer.RenderText(
            new Rect(0, 0, 640f, 640f), 
            new TextStyle
            {
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Start,
                Color = Color.FromHex(0xff00ff, 1f),
            },
            "Left"
        );
        
        TextRenderer.RenderText(
            new Rect(0, 0, 640f, 640f), 
            new TextStyle
            {
                VerticalTextAlignment = TextAlignment.End,
                HorizontalTextAlignment = TextAlignment.Start,
                Color = Color.FromHex(0xff00ff, 1f),
            },
            "Bottom Left"
        );
        
        TextRenderer.RenderText(
            new Rect(0, 0, 640f, 640f), 
            new TextStyle
            {
                VerticalTextAlignment = TextAlignment.Start,
                HorizontalTextAlignment = TextAlignment.End,
                Color = Color.FromHex(0xff00ff, 1f),
            },
            "Top Right"
        );
        
        TextRenderer.RenderText(
            new Rect(0, 0, 640f, 640f), 
            new TextStyle
            {
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                Color = Color.FromHex(0xff00ff, 1f),
            },
            "Centered"
        );

        TextRenderer.RenderText(
            new Rect(0, 0, 640f, 640f), 
            new TextStyle
            {
                HorizontalTextAlignment = TextAlignment.End,
                VerticalTextAlignment = TextAlignment.Center,
                Color = Color.FromHex(0xff00ff, 1f),
            },
            "Right"
        );
        
        TextRenderer.RenderText(
            new Rect(0, 0, 640f, 640f), 
            new TextStyle
            {
                HorizontalTextAlignment = TextAlignment.End,
                VerticalTextAlignment = TextAlignment.End,
                Color = Color.FromHex(0xff00ff, 1f),
            },
            "Bottom Right"
        );
        
        //TextRenderer.RenderText(20, 200, color,"Hello world!\nAnd this is a brand new\nline?!");
        //TextRenderer.RenderText(50, 300, Color.FromHex(0x0F0f6, 1f),"This is more text");
        //TextRenderer.RenderText(200, 240, Color.FromHex(0x2f8777, 1f),"This is EVEN, MORE, perhaps, MOST,\ntext EVER!!! Need to test overlap");
        //TextRenderer.RenderText(0, 0, Color.FromHex(0x2f8777, 1f),"Should align with bottom left corner");
        //TextRenderer.RenderText(0, 640 - TextRenderer.LineHeight, Color.FromHex(0x2f8777, 1f),"Should align with top left corner");
        //TextRenderer.RenderText(55, 290, Color.FromHex(0x2f8777, 1f),"Overlapping text");
        glFlush();
    }

    public void Unload()
    {
        TextRenderer.Dispose();   
    }
}