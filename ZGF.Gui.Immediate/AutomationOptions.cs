using System.Globalization;

namespace ZGF.Gui.Immediate;

/// <summary>Runs an <see cref="ImmediateGuiAppHost{TUiState}"/> in automation mode: its windows are never shown,
/// input, time and OS services come only from its <see cref="ImmediateAutomation"/>, and every window draws
/// offscreen. Entered only by passing these to the host, from <see cref="Parse"/> or <see cref="InProcess"/>.</summary>
public sealed class AutomationOptions
{
    /// <summary>The launch flag, followed by a port and a token, <c>--zgf-automation 47100 s3cret</c>, or by 0, a token
    /// and the file to report the port the app picks in, <c>--zgf-automation 0 s3cret /tmp/port</c>.</summary>
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
            return new AutomationLaunch.Malformed($"{Flag} takes a port and a token: {Flag} <port> <token>, or {Flag} 0 <token> <port-file>.");

        var portText = args[at + 1];
        if (!int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out var port) || port is < 0 or > 65535)
            return new AutomationLaunch.Malformed($"{Flag} port \"{portText}\" is not a whole number from 0 to 65535.");
        var token = args[at + 2];
        var tokenProblem = AutomationEndpoint.TokenProblem(token);
        if (tokenProblem != null)
            return new AutomationLaunch.Malformed($"{Flag} token {tokenProblem}.");
        if (port != 0)
            return Automated(new AutomationPort.Fixed(port), token);
        if (at + 3 >= args.Count || !Path.IsPathFullyQualified(args[at + 3]))
            return new AutomationLaunch.Malformed($"{Flag} 0 picks a port, and takes the absolute path of a file to report it in: {Flag} 0 <token> <port-file>.");
        return Automated(new AutomationPort.Chosen(args[at + 3]), token);
    }

    private static AutomationLaunch Automated(AutomationPort port, string token) =>
        new AutomationLaunch.Automated(new AutomationOptions(new AutomationEndpoint(port, token)));
}

/// <summary>Which port an app in automation mode serves its driver on.</summary>
public abstract record AutomationPort
{
    private AutomationPort()
    {
    }

    /// <summary>This port, 1 to 65535.</summary>
    public sealed record Fixed(int Number) : AutomationPort;

    /// <summary>A free port the app binds itself, then writes, as its decimal number alone, to
    /// <see cref="ReportFile"/>, which appears only once the port is bound.</summary>
    public sealed record Chosen(string ReportFile) : AutomationPort;
}

/// <summary>Where a driver in another process connects to an app in automation mode, and the secret it proves
/// itself with.</summary>
public sealed class AutomationEndpoint
{
    internal AutomationEndpoint(AutomationPort port, string token)
    {
        Port = port;
        Token = token;
    }

    public AutomationPort Port { get; }

    /// <summary>1 to 128 letters, digits, <c>-</c>, <c>.</c>, <c>_</c> and <c>~</c>, so it goes into a URL path as it
    /// is; not <c>.</c> or <c>..</c>, which a URL path drops.</summary>
    public string Token { get; }

    public override string ToString() => Port.ToString();

    internal static string? TokenProblem(string token)
    {
        if (token.Length is 0 or > 128)
            return "is 1 to 128 characters long";
        if (token is "." or "..")
            return "is not '.' or '..', which a URL path drops";
        foreach (var c in token)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c is not ('-' or '.' or '_' or '~'))
                return "may hold only letters, digits, '-', '.', '_' and '~'";
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
