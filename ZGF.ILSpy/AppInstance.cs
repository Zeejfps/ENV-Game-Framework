using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using ZGF.Desktop;

namespace ZGF.ILSpy;

/// <summary>
/// This run of the app among the runs with the same settings. The first run listens for the command
/// lines later runs hand it; each later run hands its own over, lends the first run the right to come
/// to the front, and leaves, so that one window serves them all. Only the user who started the first
/// run can reach it.
/// </summary>
public sealed class AppInstance : IDisposable
{
    private const string NamePrefix = "ZGF.ILSpy-";
    private const int HashBytes = 8;
    private const int IntBytes = 4;
    private const int MaxArguments = 4096;
    private const int MaxArgumentBytes = 1 << 20;
    private const byte Received = 1;
    private static readonly TimeSpan HandOverTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan BusyPipeWait = TimeSpan.FromMilliseconds(100);

    private readonly Mutex _claim;
    private readonly string _pipeName;
    private readonly ConcurrentQueue<CommandLine> _handed = new();
    private readonly CancellationTokenSource _stop = new();
    private Action? _wake;
    private Task? _serving;

    private AppInstance(string name)
    {
        _claim = new Mutex(false, name, out var createdNew);
        IsFirst = createdNew;
        _pipeName = name;
    }

    /// <summary>Whether no other run with these settings is open, so that this one serves those that come later.</summary>
    public bool IsFirst { get; }

    /// <summary>Claims this run among those with the settings at <paramref name="settingsPath"/>.</summary>
    public static AppInstance Claim(string settingsPath) => new(NameFor(settingsPath));

    /// <summary>The name the runs with the settings at <paramref name="settingsPath"/> meet under: the
    /// same for every spelling of the path the file system takes as one.</summary>
    private static string NameFor(string settingsPath)
    {
        var path = Path.GetFullPath(settingsPath);
        if (OperatingSystem.IsWindows())
            path = path.ToUpperInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(path));
        return NamePrefix + Convert.ToHexString(hash, 0, HashBytes);
    }

    /// <summary>Hands <paramref name="commandLine"/> to the first run and lends it the right to come to
    /// the front; false when it cannot be reached in time, and this run opens a window of its own. Waits
    /// on the thread pool, so a caller whose own thread runs the continuations is never blocked by them.</summary>
    public bool TryHandOver(CommandLine commandLine)
    {
        try
        {
            return Task.Run(() => HandOverAsync(commandLine)).GetAwaiter().GetResult();
        }
        catch (Exception e) when (IsBrokenHandOver(e))
        {
            return false;
        }
    }

    private async Task<bool> HandOverAsync(CommandLine commandLine)
    {
        using var timeout = new CancellationTokenSource(HandOverTimeout);
        var token = timeout.Token;
        await using var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await pipe.ConnectAsync(token);
        var processId = await ReadIntAsync(pipe, token);
        ForegroundRights.LendTo(processId);
        await WriteArgumentsAsync(pipe, commandLine.ListHandedArguments(), token);
        var answer = await ReadByteAsync(pipe, token);
        return answer == Received;
    }

    private static bool IsBrokenHandOver(Exception e) =>
        e is IOException or TimeoutException or OperationCanceledException or UnauthorizedAccessException or InvalidDataException;

    /// <summary>Starts taking, in the background, the command lines later runs hand over, when this is the first run.</summary>
    public void Listen()
    {
        if (!IsFirst)
            return;
        _serving = Task.Run(ServeAsync);
    }

    /// <summary>Has <paramref name="wake"/> run, from any thread, whenever a command line is handed over,
    /// and at once when some already wait.</summary>
    public void WakeWith(Action wake)
    {
        Volatile.Write(ref _wake, wake);
        if (!_handed.IsEmpty)
            wake();
    }

    /// <summary>Takes the oldest command line handed over and not taken yet.</summary>
    public bool TryTakeHanded([NotNullWhen(true)] out CommandLine? commandLine) => _handed.TryDequeue(out commandLine);

    /// <summary>Stops taking command lines; later runs then open windows of their own.</summary>
    public void StopListening() => _stop.Cancel();

    private async Task ServeAsync()
    {
        var token = _stop.Token;
        while (!token.IsCancellationRequested)
        {
            try
            {
                await ServeOneAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
            catch (Exception e) when (IsBrokenHandOver(e))
            {
                await WaitForBusyPipe(token);
            }
        }
    }

    /// <summary>Takes the command line of the next run that connects, then tells it so.</summary>
    private async Task ServeOneAsync(CancellationToken stop)
    {
        await using var pipe = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await pipe.WaitForConnectionAsync(stop);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop);
        timeout.CancelAfter(HandOverTimeout);
        var token = timeout.Token;
        await WriteIntAsync(pipe, Environment.ProcessId, token);
        var args = await ReadArgumentsAsync(pipe, token);
        _handed.Enqueue(CommandLine.Parse(args));
        Volatile.Read(ref _wake)?.Invoke();
        await pipe.WriteAsync(new[] { Received }, token);
        await pipe.FlushAsync(token);
    }

    /// <summary>Waits a moment after a connection broke, so that a pipe that stays busy is not tried in a tight loop.</summary>
    private static async Task WaitForBusyPipe(CancellationToken token)
    {
        try
        {
            await Task.Delay(BusyPipeWait, token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static async Task WriteArgumentsAsync(Stream pipe, List<string> args, CancellationToken token)
    {
        await WriteIntAsync(pipe, args.Count, token);
        foreach (var arg in args)
        {
            var bytes = Encoding.UTF8.GetBytes(arg);
            await WriteIntAsync(pipe, bytes.Length, token);
            await pipe.WriteAsync(bytes, token);
        }
        await pipe.FlushAsync(token);
    }

    private static async Task<string[]> ReadArgumentsAsync(Stream pipe, CancellationToken token)
    {
        var count = await ReadCountAsync(pipe, MaxArguments, token);
        var args = new string[count];
        for (var i = 0; i < count; i++)
        {
            var length = await ReadCountAsync(pipe, MaxArgumentBytes, token);
            var bytes = new byte[length];
            await pipe.ReadExactlyAsync(bytes, token);
            args[i] = Encoding.UTF8.GetString(bytes);
        }
        return args;
    }

    /// <summary>A count read from <paramref name="pipe"/>, which no run sends past <paramref name="max"/>.</summary>
    private static async Task<int> ReadCountAsync(Stream pipe, int max, CancellationToken token)
    {
        var count = await ReadIntAsync(pipe, token);
        if (count < 0)
            throw new InvalidDataException("A handed command line is malformed.");
        if (count > max)
            throw new InvalidDataException("A handed command line is too long.");
        return count;
    }

    private static async Task WriteIntAsync(Stream pipe, int value, CancellationToken token)
    {
        var bytes = new byte[IntBytes];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        await pipe.WriteAsync(bytes, token);
        await pipe.FlushAsync(token);
    }

    private static async Task<int> ReadIntAsync(Stream pipe, CancellationToken token)
    {
        var bytes = new byte[IntBytes];
        await pipe.ReadExactlyAsync(bytes, token);
        return BinaryPrimitives.ReadInt32LittleEndian(bytes);
    }

    private static async Task<byte> ReadByteAsync(Stream pipe, CancellationToken token)
    {
        var bytes = new byte[1];
        await pipe.ReadExactlyAsync(bytes, token);
        return bytes[0];
    }

    public void Dispose()
    {
        StopListening();
        WaitForServing();
        _claim.Dispose();
        _stop.Dispose();
    }

    private void WaitForServing()
    {
        if (_serving == null)
            return;
        _serving.Wait(HandOverTimeout);
    }
}
