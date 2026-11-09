using System.Globalization;

namespace ZGF.Gui.Immediate;

/// <summary>Runs an <see cref="ImmediateGuiAppHost{TUiState}"/> in automation mode: its windows are never shown,
/// input, time and OS services come only from its <see cref="ImmediateAutomation"/>, and every window draws
/// offscreen. Entered only by passing these to the host, from <see cref="Parse"/> or <see cref="InProcess"/>.</summary>
public sealed class AutomationOptions
{
    /// <summary>The launch flag, followed by a port and a token: <c>--zgf-automation 47100 s3cret</c>.</summary>
    public const string Flag = "--zgf-automation";

    private AutomationOptions(AutomationEndpoint? endpoint)
    {
        Endpoint = endpoint;
    }

    /// <summary>Where a driver in another process connects, from the launch flag; null when the driver runs in
    /// the app's own process.</summary>
    public AutomationEndpoint? Endpoint { get; }

    /// <summary>Automation driven from the app's own process, through <see cref="ImmediateGuiAppHost{TUiState}.Automation"/>.</summary>
    public static AutomationOptions InProcess() => new(null);

    /// <summary>Whether <paramref name="args"/> ask for automation mode, and with what; a flag given more than
    /// once, or without a valid port and token after it, is <see cref="AutomationLaunch.Malformed"/>.</summary>
    public static AutomationLaunch Parse(IReadOnlyList<string> args)
    {
        var at = -1;
        for (var i = 0; i < args.Count; i++)
        {
            if (args[i] != Flag) continue;
            if (at >= 0)
                return new AutomationLaunch.Malformed($"{Flag} is given more than once.");
            at = i;
        }
        if (at < 0)
            return new AutomationLaunch.Normal();
        if (at + 2 >= args.Count)
            return new AutomationLaunch.Malformed($"{Flag} takes a port and a token: {Flag} <port> <token>.");

        var portText = args[at + 1];
        if (!int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out var port) || port is < 1 or > 65535)
            return new AutomationLaunch.Malformed($"{Flag} port \"{portText}\" is not a whole number from 1 to 65535.");
        var token = args[at + 2];
        var tokenProblem = AutomationEndpoint.TokenProblem(token);
        if (tokenProblem != null)
            return new AutomationLaunch.Malformed($"{Flag} token {tokenProblem}.");
        return new AutomationLaunch.Automated(new AutomationOptions(new AutomationEndpoint(port, token)));
    }
}

/// <summary>Where a driver in another process connects to an app in automation mode, and the secret it proves
/// itself with.</summary>
public sealed class AutomationEndpoint
{
    internal AutomationEndpoint(int port, string token)
    {
        Port = port;
        Token = token;
    }

    /// <summary>A TCP port, 1 to 65535.</summary>
    public int Port { get; }

    /// <summary>Printable ASCII without spaces.</summary>
    public string Token { get; }

    public override string ToString() => $"port {Port}";

    internal static string? TokenProblem(string token)
    {
        if (token.Length == 0)
            return "is empty";
        foreach (var c in token)
        {
            if (c is <= ' ' or > '~')
                return "may hold only printable ASCII without spaces";
        }
        return null;
    }
}

/// <summary>What an app's launch arguments ask of automation.</summary>
public abstract record AutomationLaunch
{
    private AutomationLaunch() { }

    /// <summary>No automation flag: the app runs as usual.</summary>
    public sealed record Normal : AutomationLaunch;

    public sealed record Automated(AutomationOptions Options) : AutomationLaunch;

    /// <summary>The flag is there but cannot be used, for <see cref="Reason"/>.</summary>
    public sealed record Malformed(string Reason) : AutomationLaunch;
}
