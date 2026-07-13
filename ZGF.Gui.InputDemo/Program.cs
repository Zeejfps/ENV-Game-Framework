using System.Text;
using ZGF.Desktop;
using ZGF.Gui.Desktop;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.InputDemo;

/// <summary>
/// Launches a real window with a commit box in it and types Russian into it from code, at human
/// speed, while you watch.
///
/// On Windows the keystrokes are real OS input (<c>SendInput</c> with <c>KEYEVENTF_UNICODE</c>), so
/// they enter the app exactly where a physical keyboard's would: WM_CHAR -> GLFW's character
/// callback -> IWindow.OnText -> InputSystem -> the field. That is the whole chain the Cyrillic fix
/// added, end to end, with nothing stubbed. Elsewhere it falls back to injecting events into the
/// InputSystem, which still drives the real window but can't vouch for the GLFW callback.
///
///   dotnet run --project framework/ZGF.Gui.InputDemo
///   dotnet run --project framework/ZGF.Gui.InputDemo -- --inject   (skip SendInput)
/// </summary>
internal static class Program
{
    private const string Title = "Исправлен ввод кириллицы";
    private const string Description = "Текст берётся из события ОС, а не из кода клавиши.";
    private const string Fixed = "Готово ✅";

    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var forceInject = args.Contains("--inject");

        var config = new StartupConfig
        {
            WindowWidth = CommitBox.WindowWidth,
            WindowHeight = CommitBox.WindowHeight,
            WindowTitle = "ZGF — scripted Cyrillic typing",
        };

        var builder = GuiApp.CreateBuilder(config);
        CommitBox? box = null;

        var app = builder
            .UseContent(ctx =>
            {
                box = new CommitBox(ctx);
                return box.Build().BuildView(ctx);
            })
            .Build();

        var commitBox = box ?? throw new InvalidOperationException("content was never built");
        var input = builder.Services.Require<InputSystem>();
        var ticker = builder.Services.Require<IFrameTicker>();

        var os = forceInject ? null : OsTypeSink.TryCreate();
        ITypeSink sink = os ?? (ITypeSink)new InjectedTypeSink(input);
        Console.WriteLine($"typing via: {sink.Describe}\n");

        commitBox.FocusTitle();

        var typist = BuildScript(sink);
        typist.OnNote = note => Console.WriteLine($"  {note}");

        var settled = false;
        var warnedUnfocused = false;
        var grabbedFocus = false;
        ticker.Add(dt =>
        {
            // The window only exists once the app is running, so this is the first chance to aim at it.
            if (os != null && !grabbedFocus && os.HasWindow)
            {
                grabbedFocus = true;
                os.Focus();
            }

            // SendInput lands on whatever window the OS has focused. If that stops being ours, hold
            // the script rather than typing the rest of it into someone else's window.
            if (os != null && !os.IsTargetFocused())
            {
                if (!warnedUnfocused && !typist.Done)
                {
                    warnedUnfocused = true;
                    Console.WriteLine("  (paused — click the demo window to give it focus)");
                }
                return;
            }
            warnedUnfocused = false;

            if (typist.Update(dt))
                app.RequestRedraw();

            if (typist.Done && !settled)
            {
                settled = true;
                Console.WriteLine($"\ntitle:       {commitBox.Title}");
                Console.WriteLine($"description: {commitBox.Description}");
                Console.WriteLine("\nClose the window when you're done looking at it.");
            }
        });

        app.Run();
    }

    /// <summary>The performance: type a Cyrillic title, tab down, type a description, then go back and
    /// fix the title — the same edits a person makes, so caret motion, selection and deletion all get
    /// exercised alongside plain insertion.</summary>
    private static Typist BuildScript(ITypeSink sink) => new Typist(sink)
        .Pause(1.2f, "focus is in the commit title")
        .Type(Title, "typing the title in Russian")
        .Pause(0.9f)
        .Press(KeyboardKey.Backspace, note: "backspacing a few Cyrillic characters")
        .Repeat(KeyboardKey.Backspace, 9)
        .Pause(0.5f)
        .Type("кириллицы", "retyping them")
        .Pause(1.0f)
        .Press(KeyboardKey.Tab, note: "Tab to the description")
        .Pause(0.5f)
        .Type(Description, "typing the description")
        .Press(KeyboardKey.Enter, note: "Enter breaks the line (multi-line field)")
        .Type("Теперь можно писать по-русски. 🚀", "a second line, with an emoji")
        .Pause(1.2f)
        .Press(KeyboardKey.Tab, note: "back to the title (Shift+Tab would too)")
        .Press(KeyboardKey.A, control: true, note: "Ctrl+A to select all")
        .Pause(0.6f)
        .Type(Fixed, "replacing the selection")
        .Pause(0.5f);
}
