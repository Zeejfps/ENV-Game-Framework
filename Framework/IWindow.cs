namespace Framework;

public interface IWindow
{
    int Width { get; set; }
    int Height { get; set; }
    string Title { get; set; }
    bool IsResizable { get; set; }
    int PosX { get; set; }
    int PosY { get; set; }
    bool IsFullscreen { get; set; }
    bool IsVsyncEnabled { get; set; }
    
    bool IsOpened { get; }
    IFramebuffer Framebuffer { get; }

    void Open();
    void Close();
    void Update();
    void Resize(int width, int height);
    void Reposition(int x, int y);
}