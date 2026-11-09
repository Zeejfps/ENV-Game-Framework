using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Automation;

/// <summary>A command the app answered with something other than success: a target not pressed, a command an
/// inspection-only server does not take, or one it rejected. <see cref="Result"/> says which; the message holds the
/// snapshot a lookup failed in and the <c>Look</c> written at that moment.</summary>
public sealed class RemoteAppException(AutomationResult result, string message) : Exception(message)
{
    public AutomationResult Result { get; } = result;
}

/// <summary>
/// An immediate-mode app started in a process of its own in automation mode — hidden, on a virtual clock, with fake OS
/// services — and driven over MCP. <see cref="Launch"/> starts it with <c>--zgf-automation 0 &lt;token&gt; &lt;port-file&gt;</c>,
/// so the app binds a free port itself and reports the one it holds, with a fresh token, checks the protocol version and capabilities its server reports, and returns once it
/// is ready, as <see cref="WaitUntilReady"/> says. The operations it shares with an in-process <c>GpuFrames</c> have its
/// names; a lookup that fails or a wait that times out throws with the snapshot of that moment and a <c>Look</c> of it.
/// Dispose closes the app, and kills it if it does not exit. One thread drives it.
/// </summary>
public sealed class RemoteApp : IDisposable
{
    private static readonly TimeSpan CallMargin = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(5);

    private readonly Process _process;
    private readonly McpHttpClient _client;
    private readonly StringBuilder _output;
    private readonly Action<string>? _report;
    private bool _disposed;

    private RemoteApp(Process process, McpHttpClient client, StringBuilder output, Uri endpoint, Action<string>? report)
    {
        _process = process;
        _client = client;
        _output = output;
        Endpoint = endpoint;
        _report = report;
        ScratchDirectory = ScratchFolder.Find();
    }

    /// <summary>Where the app's server listens, its token included.</summary>
    public Uri Endpoint { get; }

    public int ProcessId => _process.Id;

    /// <summary>Where <c>Look</c> writes: <c>scratch/</c> at the root of the repository the tests were built from, else
    /// in the temp folder.</summary>
    public string ScratchDirectory { get; set; }

    /// <summary>How long a command that sets no timeout of its own may take, frames and transport included.</summary>
    public TimeSpan CallTimeout { get; set; } = TimeSpan.FromSeconds(120);

    /// <summary>What the app wrote to its standard output and error so far.</summary>
    public string Output
    {
        get
        {
            lock (_output)
                return _output.ToString();
        }
    }

    /// <summary>Starts the app <paramref name="launch"/> names in automation mode and returns once it is ready.</summary>
    /// <param name="report">Where <c>Look</c> reports the files it writes, besides the console, such as an xUnit test's
    /// output.</param>
    /// <exception cref="TimeoutException">Its server did not answer within <see cref="RemoteLaunch.ConnectTimeout"/>, or
    /// it was not ready within <see cref="RemoteLaunch.ReadyTimeout"/>.</exception>
    /// <exception cref="InvalidOperationException">It exited first, or its server speaks another protocol version or does
    /// not drive the app.</exception>
    public static RemoteApp Launch(RemoteLaunch launch, Action<string>? report = null)
    {
        var deadline = Stopwatch.StartNew();
        var token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
        var portFile = Path.Combine(Path.GetTempPath(), $"zgf-automation-port-{Guid.NewGuid():N}");
        var output = new StringBuilder();
        var process = Start(launch, token, portFile, output);
        McpHttpClient? client = null;
        Uri endpoint;
        try
        {
            var port = WaitForPort(process, portFile, output, launch.ConnectTimeout, deadline);
            endpoint = new Uri($"http://127.0.0.1:{port}/mcp/{token}");
            client = new McpHttpClient(endpoint);
            Connect(process, client, output, launch.ConnectTimeout, deadline);
        }
        catch
        {
            Abandon(process, client);
            throw;
        }
        finally
        {
            ForgetPortFile(portFile, output);
        }
        var app = new RemoteApp(process, client, output, endpoint, report);
        try
        {
            app.CheckCapabilities(Remaining(launch.ConnectTimeout, deadline));
            app.WaitUntilReady(launch.ReadyTimeout);
            return app;
        }
        catch
        {
            app.Dispose();
            throw;
        }
    }

    /// <summary>Puts the pointer at (x, y) in the main window's UI points and runs a frame.</summary>
    public RemoteApp MovePointer(float x, float y) => MovePointer(new WindowPoint(x, y));

    public RemoteApp MovePointer(WindowPoint at, int frames = 1) => Do(new AutomationCommand.MovePointer(at, frames));

    public RemoteApp Press(PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None) =>
        Do(new AutomationCommand.PointerDown(button, modifiers));

    public RemoteApp Release(PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None) =>
        Do(new AutomationCommand.PointerUp(button, modifiers));

    /// <summary>Moves to (x, y), lets a frame see the pointer there, then presses and releases over two more frames.</summary>
    public RemoteApp Click(float x, float y, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None) =>
        Do(new AutomationCommand.Click(new WindowPoint(x, y), button, modifiers));

    /// <summary>Presses at <paramref name="from"/>, moves to <paramref name="to"/> in <paramref name="steps"/> steps a frame
    /// apart, and releases there.</summary>
    public RemoteApp DragAndRelease(WindowPoint from, WindowPoint to, int steps = 4, PointerButton button = PointerButton.Left,
        KeyModifiers modifiers = KeyModifiers.None) =>
        Do(new AutomationCommand.Drag(from, to, steps, button, modifiers));

    /// <summary>Scrolls the wheel where the pointer is and runs a frame.</summary>
    public RemoteApp Wheel(double y, double x = 0) => Do(new AutomationCommand.Scroll(x, y));

    /// <summary>Presses and releases <paramref name="key"/> and runs a frame.</summary>
    public RemoteApp Key(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None) => Do(new AutomationCommand.Key(key, modifiers));

    public RemoteApp ReleaseKey(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None) =>
        Do(new AutomationCommand.Key(key, modifiers, KeyAction.Up));

    public RemoteApp Type(string text) => Do(new AutomationCommand.TypeText(text));

    public RemoteApp Compose(string text, int? caret = null) => Do(new AutomationCommand.Compose(text, caret));

    /// <summary>Moves the pointer onto the node <paramref name="selector"/> names, at a point a press reaches it, and runs
    /// a frame; throws a <see cref="RemoteAppException"/>, with a <c>Look</c> of that moment, moving nothing, when no such
    /// point exists, or when after that frame a press there would no longer reach it.</summary>
    public RemoteApp MoveTo(Selector selector) => Do(new AutomationCommand.MoveTo(selector));

    /// <summary>As <see cref="MoveTo"/>, then a press and a release over two more frames.</summary>
    public RemoteApp ClickOn(Selector selector, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None) =>
        Do(new AutomationCommand.ClickOn(selector, button, modifiers));

    /// <summary>Runs <paramref name="count"/> frames without moving the virtual clock.</summary>
    public RemoteApp Step(int count = 1) => Do(new AutomationCommand.Step(count));

    /// <summary>Moves the virtual clock on by <paramref name="seconds"/>, a frame every 1/60 s.</summary>
    public RemoteApp Advance(double seconds) => Do(new AutomationCommand.Advance(seconds));

    /// <summary>Runs frames 1/60 s apart until one ends with nothing but ambient updates pending, or
    /// <paramref name="maxSeconds"/> of virtual time have passed.</summary>
    public SettleResult Settle(double maxSeconds = 5) => Payload<AutomationPayload.Settled>(new AutomationCommand.Settle(maxSeconds)).Result;

    /// <summary>Runs frames until posted work, requested updates and the app's tracked background work are done; throws
    /// a <see cref="TimeoutException"/>, with a <c>Look</c> of that moment, once <paramref name="timeout"/> has passed.</summary>
    public RemoteApp WaitUntilReady(TimeSpan timeout) => Do(new AutomationCommand.WaitUntilReady(timeout));

    /// <summary>Runs frames until exactly one node matches <paramref name="selector"/>, and returns it; the
    /// <see cref="TimeoutException"/> says what was found instead and holds a <c>Look</c> of that moment.</summary>
    public NodeSummary WaitFor(Selector selector, TimeSpan timeout) =>
        Payload<AutomationPayload.Node>(new AutomationCommand.WaitFor(selector, timeout)).Found;

    /// <summary>The last frame as text, as <see cref="SemanticSnapshot.ToText"/> writes it.</summary>
    public string Snapshot() => Payload<AutomationPayload.Snapshot>(new AutomationCommand.Snapshot()).Text;

    /// <summary>A PNG of what <paramref name="target"/> names in the last frame, and what it shows.</summary>
    public AutomationPayload.Image Capture(CaptureTarget target) => Payload<AutomationPayload.Image>(new AutomationCommand.Capture(target));

    /// <summary>Writes the last frame's snapshot to <c>scratch/&lt;name&gt;.txt</c>, and the main window with every popup
    /// and window over it to <c>scratch/&lt;name&gt;.png</c>, and reports both paths.</summary>
    public LookFiles Look(string name) => LookAt(name, new CaptureTarget.Composite());

    /// <summary>As <see cref="Look(string)"/>, of the node <paramref name="selector"/> names: its subtree, and the pixels
    /// its own surface drew around it, <paramref name="margin"/> points beyond its bounds, each pixel
    /// <paramref name="magnify"/> times as wide and high.</summary>
    public LookFiles Look(string name, Selector selector, float margin = 8f, int magnify = 1) =>
        LookAt(name, new CaptureTarget.Element(selector, margin, magnify));

    /// <summary>As <see cref="Look(string)"/>, of surface <paramref name="surface"/> alone, as the snapshot lists them.</summary>
    public LookFiles Look(string name, int surface) => LookAt(name, new CaptureTarget.Surface(surface));

    /// <summary>Answers the next file picker the app opens with <paramref name="path"/>. Pickers with no answer queued cancel.</summary>
    public RemoteApp QueueFilePickerAnswer(string path) => Do(new AutomationCommand.QueueFilePicker(path));

    public RemoteApp QueueFilePickerCancel() => Do(new AutomationCommand.QueueFilePicker(null));

    public IReadOnlyList<FilePickerRequest> FilePickerRequests() =>
        Payload<AutomationPayload.FilePickers>(new AutomationCommand.FilePickerRequests()).Requests;

    public string? GetClipboardText() => Payload<AutomationPayload.Clipboard>(new AutomationCommand.GetClipboard()).Text;

    public string? GetClipboardHtml() => Payload<AutomationPayload.Clipboard>(new AutomationCommand.GetClipboard()).Html;

    public RemoteApp SetClipboardText(string text) => Do(new AutomationCommand.SetClipboard(text));

    public IReadOnlyList<SystemMenuItem> SystemMenuItems() => Payload<AutomationPayload.MenuItems>(new AutomationCommand.SystemMenuItems()).Items;

    public RemoteApp PickSystemMenuItem(params string[] path) => Do(new AutomationCommand.PickSystemMenuItem(path));

    /// <summary>Every platform effect the app asked for, in order, recorded instead of performed.</summary>
    public IReadOnlyList<AutomationEffect> Effects() => Payload<AutomationPayload.Effects>(new AutomationCommand.Effects()).Recorded;

    /// <summary>Closes the app as its close button would, and waits for its process to exit.</summary>
    public void Close()
    {
        Do(new AutomationCommand.Close());
        if (!_process.WaitForExit(CloseTimeout))
            throw new TimeoutException($"The app did not exit within {CloseTimeout.TotalSeconds:0}s of closing.\n{Output}");
    }

    /// <summary>Sends <paramref name="command"/> and returns what the app answered, success or not.</summary>
    /// <exception cref="InvalidOperationException">The app has exited.</exception>
    /// <exception cref="McpProtocolException">Its server answered with something other than a result, or with a result
    /// whose isError disagrees with its status.</exception>
    /// <exception cref="McpToolFailedException">The command failed inside the server, before it could answer.</exception>
    public AutomationResult Send(AutomationCommand command) => Send(command, TimeoutFor(command));

    private AutomationResult Send(AutomationCommand command, TimeSpan timeout)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var (name, arguments) = AutomationWire.Write(command);
        ToolCallReply reply;
        try
        {
            reply = _client.CallTool(name, arguments, timeout);
        }
        catch (HttpRequestException e)
        {
            throw Exited(e);
        }
        return ToolReplies.Read(name, reply);
    }

    /// <summary>Asks the app to close, then, whatever came of that, kills its process tree if it is still running and
    /// waits for it to go.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        try
        {
            if (!_process.HasExited)
            {
                // A close the app does not answer, or answers as it goes, changes nothing: the kill below guarantees it is gone.
                try
                {
                    _client.CallTool("gui_close", "{}", CloseTimeout);
                }
                catch (Exception e) when (e is HttpRequestException or TimeoutException or McpProtocolException)
                {
                }
                _process.WaitForExit(CloseTimeout);
            }
        }
        finally
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                    _process.WaitForExit(CloseTimeout);
                }
            }
            finally
            {
                _client.Dispose();
                _process.Dispose();
            }
        }
    }

    private RemoteApp Do(AutomationCommand command)
    {
        Expect(command);
        return this;
    }

    private T Payload<T>(AutomationCommand command, TimeSpan? timeout = null) where T : AutomationPayload
    {
        var ok = Expect(command, timeout);
        if (ok.Payload is not T payload)
            throw new McpProtocolException($"{AutomationWire.Write(command).Name} answered with {ok.Payload.GetType().Name}, not {typeof(T).Name}.");
        return payload;
    }

    /// <summary>Sends <paramref name="command"/>; anything but success throws, a failed lookup or a timeout with a
    /// <c>Look</c> of that moment.</summary>
    private AutomationResult.Ok Expect(AutomationCommand command, TimeSpan? timeout = null)
    {
        var result = Send(command, timeout ?? TimeoutFor(command));
        switch (result)
        {
            case AutomationResult.Ok ok:
                return ok;
            case AutomationResult.Timeout:
                throw new TimeoutException(LookAtFailure() + "\n" + AutomationWire.Text(result));
            case AutomationResult.NotFound or AutomationResult.Ambiguous or AutomationResult.Occluded or AutomationResult.Blocked
                or AutomationResult.Clipped or AutomationResult.Unreachable:
                throw new RemoteAppException(result, LookAtFailure() + "\n" + AutomationWire.Text(result));
            case AutomationResult.Unsupported or AutomationResult.Rejected:
                throw new RemoteAppException(result, $"{AutomationWire.Write(command).Name}: {result.FailureMessage}");
            default:
                throw new ArgumentOutOfRangeException(nameof(command), result, "An unknown result.");
        }
    }

    /// <summary>A note on the Look written at a failure, or why none could be; never replaces the failure itself.</summary>
    private string LookAtFailure()
    {
        try
        {
            var result = Send(new AutomationCommand.Capture(new CaptureTarget.Composite()));
            if (result is not AutomationResult.Ok ok || ok.Payload is not AutomationPayload.Image image)
                return $"No look at the moment it failed: {result.FailureMessage}";
            var files = Write($"failure-frame{ok.Frame}", image);
            return $"Look at the moment it failed:\n{files}";
        }
        catch (Exception e) when (e is InvalidOperationException or McpProtocolException or McpToolFailedException or TimeoutException
                                      or IOException or UnauthorizedAccessException)
        {
            return $"No look at the moment it failed: {e.GetType().Name}: {e.Message}";
        }
    }

    private LookFiles LookAt(string name, CaptureTarget target)
    {
        LookArguments.RequireFileName(name);
        return Write(name, Capture(target));
    }

    private LookFiles Write(string name, AutomationPayload.Image image)
    {
        LookArguments.RequireFileName(name);
        Directory.CreateDirectory(ScratchDirectory);
        var files = new LookFiles(Path.GetFullPath(Path.Combine(ScratchDirectory, name + ".txt")),
            Path.GetFullPath(Path.Combine(ScratchDirectory, name + ".png")));
        File.WriteAllText(files.Text, image.Text);
        File.WriteAllBytes(files.Png, image.Png);
        var report = $"Look \"{name}\":\n  {files.Text}\n  {files.Png}";
        Console.WriteLine(report);
        _report?.Invoke(report);
        return files;
    }

    private TimeSpan TimeoutFor(AutomationCommand command) => command switch
    {
        AutomationCommand.WaitFor waitFor => waitFor.Timeout + CallMargin,
        AutomationCommand.WaitUntilReady ready => ready.Timeout + CallMargin,
        _ => CallTimeout,
    };

    private void CheckCapabilities(TimeSpan budget)
    {
        var capabilities = Payload<AutomationPayload.Capabilities>(new AutomationCommand.Capabilities(), budget);
        if (capabilities.Version != AutomationWire.ProtocolVersion)
            throw new InvalidOperationException(
                $"The app speaks automation protocol {capabilities.Version}; this RemoteApp speaks {AutomationWire.ProtocolVersion}. Rebuild one against the other.");
        if (capabilities.Set != AutomationCapabilitySet.Automation)
            throw new InvalidOperationException($"The app's server offers {capabilities.Set} only, and RemoteApp drives the app.");
        var missing = AutomationWire.Names(AutomationCapabilitySet.Automation).Except(capabilities.Commands).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"The app's server lacks {string.Join(", ", missing)}.");
    }

    private InvalidOperationException Exited(Exception cause)
    {
        _process.WaitForExit(CloseTimeout);
        var state = _process.HasExited ? $"exited with code {_process.ExitCode}" : "stopped answering";
        return new InvalidOperationException($"The app {state}.\n{Output}", cause);
    }

    private static Process Start(RemoteLaunch launch, string token, string portFile, StringBuilder output)
    {
        var info = new ProcessStartInfo
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = launch.WorkingDirectory ?? "",
        };
        switch (launch.Program)
        {
            case RemoteProgram.Dll dll:
                info.FileName = DotnetHost();
                info.ArgumentList.Add(dll.Path);
                break;
            case RemoteProgram.Executable executable:
                info.FileName = executable.Path;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(launch), launch.Program, null);
        }
        foreach (var argument in launch.Arguments)
            info.ArgumentList.Add(argument);
        info.ArgumentList.Add(AutomationOptions.Flag);
        info.ArgumentList.Add("0");
        info.ArgumentList.Add(token);
        info.ArgumentList.Add(portFile);
        foreach (var (key, value) in launch.Environment)
            info.Environment[key] = value;

        var process = new Process { StartInfo = info };
        process.OutputDataReceived += (_, e) => Append(output, e.Data);
        process.ErrorDataReceived += (_, e) => Append(output, e.Data);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static void Append(StringBuilder output, string? line)
    {
        if (line == null)
            return;
        lock (output)
            output.AppendLine(line);
    }

    /// <summary>The <c>dotnet</c> host the SDK runs this process under when it says, else the one on the PATH.</summary>
    private static string DotnetHost()
    {
        var host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrEmpty(host) && File.Exists(host))
            return host;
        return "dotnet";
    }

    /// <summary>The port the app bound and wrote to <paramref name="portFile"/>, which appears whole once it is bound.</summary>
    private static int WaitForPort(Process process, string portFile, StringBuilder output, TimeSpan timeout, Stopwatch elapsed)
    {
        while (true)
        {
            if (File.Exists(portFile))
            {
                var text = File.ReadAllText(portFile).Trim();
                if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var port) || port is < 1 or > 65535)
                    throw new InvalidOperationException($"The app reported the port \"{text}\", which is none.");
                return port;
            }
            ThrowIfGone(process, output, timeout, elapsed, "reported its port");
            Thread.Sleep(10);
        }
    }

    /// <summary>Opens the MCP session, retrying only while the server cannot be reached yet, all within one deadline.</summary>
    private static void Connect(Process process, McpHttpClient client, StringBuilder output, TimeSpan timeout, Stopwatch elapsed)
    {
        while (true)
        {
            ThrowIfGone(process, output, timeout, elapsed, "answered");
            try
            {
                client.Initialize(timeout - elapsed.Elapsed);
                return;
            }
            catch (HttpRequestException unreachable) when (NotUpYet(unreachable))
            {
                Thread.Sleep(20);
            }
        }
    }

    private static bool NotUpYet(HttpRequestException e)
    {
        if (e.HttpRequestError == HttpRequestError.ConnectionError)
            return true;
        return e.InnerException is SocketException socket
               && socket.SocketErrorCode is SocketError.ConnectionRefused or SocketError.ConnectionReset;
    }

    private static void ThrowIfGone(Process process, StringBuilder output, TimeSpan timeout, Stopwatch elapsed, string what)
    {
        if (process.HasExited)
        {
            process.WaitForExit();
            throw new InvalidOperationException($"The app exited with code {process.ExitCode} before its server {what}.\n{Text(output)}");
        }
        if (elapsed.Elapsed >= timeout)
            throw new TimeoutException($"The app's server had not {what} within {timeout.TotalSeconds:0.#}s.\n{Text(output)}");
    }

    private static string Text(StringBuilder output)
    {
        lock (output)
            return output.ToString();
    }

    /// <summary>The connect budget <paramref name="elapsed"/> has left, which the capability check spends.</summary>
    private static TimeSpan Remaining(TimeSpan timeout, Stopwatch elapsed)
    {
        var left = timeout - elapsed.Elapsed;
        if (left <= TimeSpan.Zero)
            throw new TimeoutException($"The app's server answered, but no time of the {timeout.TotalSeconds:0.#}s connect budget is left to check its capabilities.");
        return left;
    }

    /// <summary>Deletes the port file the app reported in. Losing it is no reason to abandon an app that answered, so a
    /// file that cannot be deleted is noted in the app's output instead.</summary>
    private static void ForgetPortFile(string portFile, StringBuilder output)
    {
        try
        {
            File.Delete(portFile);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Append(output, $"[RemoteApp] could not delete the port file {portFile}: {e.Message}");
        }
    }

    private static void Abandon(Process process, McpHttpClient? client)
    {
        client?.Dispose();
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(CloseTimeout);
            }
        }
        finally
        {
            process.Dispose();
        }
    }
}
