using static GL46;

namespace OpenGLSandbox;

public sealed class BitmapFontRenderingScene : IScene
{
    private TextRenderer TextRenderer { get; set; } = new TextRenderer();
    
    public void Load()
    {
        TextRenderer.Load();
        glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
        glClearColor(0f, 0.3f, 0.6f, 1f);
    }

    public void Update()
    {
        glClear(GL_COLOR_BUFFER_BIT);

        TextRenderer.Clear();
        
        TextRenderer.DrawText(
            new Rect(0, 0, 640f, 640f), 
            new TextStyle
            {
                VerticalTextAlignment = TextAlignment.Start,
                HorizontalTextAlignment = TextAlignment.Start,
                Color = Color.FromHex(0xff0000, 1f),
            },
            "Top Left"
        );
        
        TextRenderer.DrawText(
            new Rect(0, 0, 640f, 640f), 
            new TextStyle
            {
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Start,
                Color = Color.FromHex(0xff00ff, 1f),
            },
            "Left"
        );
        
        TextRenderer.DrawText(
            new Rect(0, 0, 640f, 640f), 
            new TextStyle
            {
                VerticalTextAlignment = TextAlignment.End,
                HorizontalTextAlignment = TextAlignment.Start,
                Color = Color.FromHex(0x00FFFF, 1f),
            },
            "Bottom Left"
        );
        
        TextRenderer.DrawText(
            new Rect(0, 0, 640f, 640f), 
            new TextStyle
            {
                VerticalTextAlignment = TextAlignment.Start,
                HorizontalTextAlignment = TextAlignment.End,
                Color = Color.FromHex(0xF4D35E, 1f),
            },
            "Top Right"
        );
        
        TextRenderer.DrawText(
            new Rect(0, 0, 640f, 640f), 
            new TextStyle
            {
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                Color = Color.FromHex(0x99FF99, 1f),
            },
            "Centered"
        );

        TextRenderer.DrawText(
            new Rect(0, 0, 640f, 640f), 
            new TextStyle
            {
                HorizontalTextAlignment = TextAlignment.End,
                VerticalTextAlignment = TextAlignment.Center,
                Color = Color.FromHex(0xF78764, 1f),
            },
            "Right"
        );
        
        TextRenderer.DrawText(
            new Rect(0, 0, 640f, 640f), 
            new TextStyle
            {
                HorizontalTextAlignment = TextAlignment.End,
                VerticalTextAlignment = TextAlignment.End,
                Color = Color.FromHex(0xEBEBD3, 1f),
            },
            "Bottom Right"
        );
        
        //TextRenderer.RenderText(20, 200, color,"Hello world!\nAnd this is a brand new\nline?!");
        //TextRenderer.RenderText(50, 300, Color.FromHex(0x0F0f6, 1f),"This is more text");
        //TextRenderer.RenderText(200, 240, Color.FromHex(0x2f8777, 1f),"This is EVEN, MORE, perhaps, MOST,\ntext EVER!!! Need to test overlap");
        //TextRenderer.RenderText(0, 0, Color.FromHex(0x2f8777, 1f),"Should align with bottom left corner");
        //TextRenderer.RenderText(0, 640 - TextRenderer.LineHeight, Color.FromHex(0x2f8777, 1f),"Should align with top left corner");
        //TextRenderer.RenderText(55, 290, Color.FromHex(0x2f8777, 1f),"Overlapping text");
        
        TextRenderer.Render();
    }

    public void Unload()
    {
        TextRenderer.Dispose();   
    }
}