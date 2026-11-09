using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ZGF.Gui.Automation;

/// <summary>A tool call's answer as the server sent it: its structured content, the PNG an image block carried, and
/// the text of its first block, which is all a call that failed inside the server carries.</summary>
internal sealed record ToolCallReply(JsonElement? Structured, byte[]? Png, bool IsError, string Text);

/// <summary>The MCP server answered with something other than a well-formed JSON-RPC result.</summary>
public sealed class McpProtocolException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>A tool failed inside the server, before it could answer in its own structured form.</summary>
public sealed class McpToolFailedException(string tool, string message) : Exception($"{tool} failed inside the server: {message}")
{
    public string Tool { get; } = tool;
}

/// <summary>
/// The client half of MCP's Streamable HTTP transport, as much as calling tools takes: <c>initialize</c>, capturing
/// the session id and the protocol version the server chose, <c>notifications/initialized</c>, <c>tools/call</c>, the
/// <c>notifications/cancelled</c> a call that runs out of time sends, and the <c>DELETE</c> that ends the session. An
/// answer comes as JSON or as an event stream, which is read as it arrives until the response to the request; anything
/// else fails as an <see cref="McpProtocolException"/>. One call at a time.
/// </summary>
internal sealed class McpHttpClient : IDisposable
{
    /// <summary>The protocol versions this client speaks, newest first.</summary>
    public static readonly IReadOnlyList<string> ProtocolVersions = ["2025-06-18", "2025-03-26"];

    private static readonly TimeSpan CancelNoticeTimeout = TimeSpan.FromSeconds(2);

    private readonly HttpClient _http = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly Uri _endpoint;
    private string? _sessionId;
    private string? _protocolVersion;
    private int _nextId = 1;

    public McpHttpClient(Uri endpoint)
    {
        _endpoint = endpoint;
    }

    /// <summary>The protocol version the server chose on initialize.</summary>
    public string? ProtocolVersion => _protocolVersion;

    /// <summary>Opens a session; the server's <c>initialize</c> result.</summary>
    /// <exception cref="HttpRequestException">Nothing answered at the endpoint.</exception>
    /// <exception cref="McpProtocolException">The server answered, but not as an MCP server this client speaks to.</exception>
    public JsonElement Initialize(TimeSpan timeout)
    {
        var deadline = Stopwatch.StartNew();
        var id = _nextId++;
        var body = Request(id, "initialize", w =>
        {
            w.WriteString("protocolVersion", ProtocolVersions[0]);
            w.WriteStartObject("capabilities");
            w.WriteEndObject();
            w.WriteStartObject("clientInfo");
            w.WriteString("name", "ZGF RemoteApp");
            w.WriteString("version", "1");
            w.WriteEndObject();
        });
        var (result, headers) = Exchange(body, id, timeout);
        var chosen = result.TryGetProperty("protocolVersion", out var version) && version.ValueKind == JsonValueKind.String
            ? version.GetString()
            : null;
        if (chosen == null || !ProtocolVersions.Contains(chosen))
            throw new McpProtocolException($"The server chose protocol version \"{chosen}\", which this client does not speak: {string.Join(", ", ProtocolVersions)}.");
        if (!headers.TryGetValues("Mcp-Session-Id", out var ids))
            throw new McpProtocolException("The server opened no session: initialize came back without Mcp-Session-Id.");
        _sessionId = ids.Single();
        _protocolVersion = chosen;
        Notify("notifications/initialized", w => { }, Left(timeout, deadline));
        return result;
    }

    /// <summary>Calls <paramref name="name"/> with <paramref name="argumentsJson"/>; once <paramref name="timeout"/> passes
    /// without an answer, tells the server the request is cancelled and throws a <see cref="TimeoutException"/>.</summary>
    public ToolCallReply CallTool(string name, string argumentsJson, TimeSpan timeout)
    {
        var id = _nextId++;
        var body = Request(id, "tools/call", w =>
        {
            w.WriteString("name", name);
            w.WritePropertyName("arguments");
            w.WriteRawValue(argumentsJson);
        });
        JsonElement result;
        try
        {
            (result, _) = Exchange(body, id, timeout);
        }
        catch (TimeoutException)
        {
            CancelQuietly(id);
            throw;
        }
        return ToolReply(result);
    }

    /// <summary>Ends the session, if the server is still there to hear it; a server already gone has ended it.</summary>
    public void EndSession(TimeSpan timeout)
    {
        if (_sessionId == null)
            return;
        using var request = new HttpRequestMessage(HttpMethod.Delete, _endpoint);
        AddSessionHeaders(request);
        using var cancel = new CancellationTokenSource(timeout);
        try
        {
            using var response = _http.Send(request, cancel.Token);
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException)
        {
        }
        _sessionId = null;
    }

    public void Dispose() => _http.Dispose();

    /// <summary>Tells the server request <paramref name="id"/> is cancelled, so it drops it if it has not run it. Best
    /// effort: the call has already failed, and a server that cannot be told has nothing left to cancel.</summary>
    private void CancelQuietly(int id)
    {
        try
        {
            Notify("notifications/cancelled", w =>
            {
                w.WriteNumber("requestId", id);
                w.WriteString("reason", "The client stopped waiting.");
            }, CancelNoticeTimeout);
        }
        catch (Exception e) when (e is HttpRequestException or TimeoutException or McpProtocolException)
        {
        }
    }

    private static ToolCallReply ToolReply(JsonElement result)
    {
        try
        {
            var isError = false;
            if (result.TryGetProperty("isError", out var error))
            {
                isError = error.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => throw new McpProtocolException($"isError is {error.ValueKind}, not a boolean."),
                };
            }
            JsonElement? structured = null;
            if (result.TryGetProperty("structuredContent", out var content))
            {
                if (content.ValueKind != JsonValueKind.Object)
                    throw new McpProtocolException("structuredContent is not an object.");
                structured = content.Clone();
            }
            byte[]? png = null;
            var text = "";
            if (!result.TryGetProperty("content", out var blocks) || blocks.ValueKind != JsonValueKind.Array)
                throw new McpProtocolException("A tool result has no content array.");
            foreach (var block in blocks.EnumerateArray())
            {
                var type = block.GetProperty("type").GetString();
                if (type == "text" && text.Length == 0)
                    text = block.GetProperty("text").GetString() ?? "";
                else if (type == "image")
                {
                    if (png != null)
                        throw new McpProtocolException("A tool result carries more than one image.");
                    if (block.GetProperty("mimeType").GetString() != "image/png")
                        throw new McpProtocolException("A tool result carries an image that is not a PNG.");
                    png = Convert.FromBase64String(block.GetProperty("data").GetString() ?? "");
                }
            }
            return new ToolCallReply(structured, png, isError, text);
        }
        catch (Exception e) when (e is FormatException or KeyNotFoundException or InvalidOperationException)
        {
            throw new McpProtocolException($"A malformed tool result: {e.Message}", e);
        }
    }

    /// <summary>Posts <paramref name="body"/> and reads the response to request <paramref name="id"/>, from a JSON body or
    /// from an event stream as its events arrive, all within <paramref name="timeout"/>.</summary>
    private (JsonElement Result, HttpResponseHeaders Headers) Exchange(string body, int id, TimeSpan timeout)
    {
        using var cancel = new CancellationTokenSource(timeout);
        try
        {
            using var request = Post(body);
            using var response = _http.Send(request, HttpCompletionOption.ResponseHeadersRead, cancel.Token);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var error = ReadBody(response, cancel.Token);
                throw new McpProtocolException($"The server answered {(int)response.StatusCode} {response.StatusCode}: {error}");
            }
            var message = response.Content.Headers.ContentType?.MediaType == "text/event-stream"
                ? FromEventStream(response, id, cancel.Token)
                : ReadBody(response, cancel.Token);
            return (Result(message, id), response.Headers);
        }
        catch (OperationCanceledException e) when (cancel.IsCancellationRequested)
        {
            throw new TimeoutException($"The MCP server did not answer within {timeout.TotalSeconds:0.#}s.", e);
        }
    }

    private void Notify(string method, Action<Utf8JsonWriter> writeParams, TimeSpan timeout)
    {
        using var cancel = new CancellationTokenSource(timeout);
        try
        {
            using var request = Post(Notification(method, writeParams));
            using var response = _http.Send(request, cancel.Token);
            if (response.StatusCode != HttpStatusCode.Accepted)
                throw new McpProtocolException($"{method} answered {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException e) when (cancel.IsCancellationRequested)
        {
            throw new TimeoutException($"The MCP server did not take {method} within {timeout.TotalSeconds:0.#}s.", e);
        }
    }

    private HttpRequestMessage Post(string body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        AddSessionHeaders(request);
        return request;
    }

    private void AddSessionHeaders(HttpRequestMessage request)
    {
        if (_sessionId == null || _protocolVersion == null)
            return;
        request.Headers.Add("Mcp-Session-Id", _sessionId);
        request.Headers.Add("MCP-Protocol-Version", _protocolVersion);
    }

    private static string ReadBody(HttpResponseMessage response, CancellationToken cancel)
    {
        using var stream = response.Content.ReadAsStream(cancel);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEndAsync(cancel).GetAwaiter().GetResult();
    }

    /// <summary>Reads events off the open stream as they arrive, returning the data of the first that answers request
    /// <paramref name="id"/> without waiting for the server to close the stream.</summary>
    private static string FromEventStream(HttpResponseMessage response, int id, CancellationToken cancel)
    {
        using var stream = response.Content.ReadAsStream(cancel);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return ReadEventStream(reader, id, cancel);
    }

    /// <summary>The data of the first event in <paramref name="reader"/> that answers request <paramref name="id"/>, read
    /// as the event-stream format says: lines end in CR, LF or CRLF; a line starting with a colon is a comment; a field
    /// is the text before the first colon, its value the rest with one leading space dropped, or empty when there is no
    /// colon; <c>data</c> values join with LF; a blank line ends an event; an event the stream ends inside is dropped.</summary>
    internal static string ReadEventStream(TextReader reader, int id, CancellationToken cancel)
    {
        var data = new StringBuilder();
        var hasData = false;
        while (true)
        {
            var line = reader.ReadLineAsync(cancel).AsTask().GetAwaiter().GetResult();
            if (line == null)
                throw new McpProtocolException($"The event stream ended without a response to request {id}.");
            if (line.Length == 0)
            {
                if (!hasData)
                    continue;
                var message = data.ToString();
                data.Clear();
                hasData = false;
                if (Answers(message, id))
                    return message;
                continue;
            }
            if (line[0] == ':')
                continue;
            var colon = line.IndexOf(':');
            var field = colon < 0 ? line : line[..colon];
            if (field != "data")
                continue;
            var value = colon < 0 ? "" : line[(colon + 1)..];
            if (value.StartsWith(' '))
                value = value[1..];
            if (hasData)
                data.Append('\n');
            data.Append(value);
            hasData = true;
        }
    }


    /// <summary>The <c>result</c> of <paramref name="json"/>, the JSON-RPC response to request <paramref name="id"/>.</summary>
    private static JsonElement Result(string json, int id)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("jsonrpc", out var version) || version.GetString() != "2.0")
                throw new McpProtocolException($"Not a JSON-RPC 2.0 message: {json}");
            if (!root.TryGetProperty("id", out var answered) || answered.ValueKind != JsonValueKind.Number || !answered.TryGetInt32(out var number) || number != id)
                throw new McpProtocolException($"Expected the response to request {id}: {json}");
            if (root.TryGetProperty("error", out var error))
                throw new McpProtocolException($"The server failed request {id}: {error}");
            if (!root.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Object)
                throw new McpProtocolException($"The response to request {id} has no result: {json}");
            return result.Clone();
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException)
        {
            throw new McpProtocolException($"The server answered with something other than JSON-RPC: {json}", e);
        }
    }

    private static bool Answers(string message, int id)
    {
        try
        {
            using var document = JsonDocument.Parse(message);
            return document.RootElement.ValueKind == JsonValueKind.Object
                   && document.RootElement.TryGetProperty("id", out var answered)
                   && answered.ValueKind == JsonValueKind.Number && answered.TryGetInt32(out var number) && number == id;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static TimeSpan Left(TimeSpan timeout, Stopwatch elapsed)
    {
        var left = timeout - elapsed.Elapsed;
        return left > TimeSpan.Zero ? left : TimeSpan.Zero;
    }

    private static string Request(int id, string method, Action<Utf8JsonWriter> writeParams) => Message(w =>
    {
        w.WriteNumber("id", id);
        w.WriteString("method", method);
        w.WriteStartObject("params");
        writeParams(w);
        w.WriteEndObject();
    });

    private static string Notification(string method, Action<Utf8JsonWriter> writeParams) => Message(w =>
    {
        w.WriteString("method", method);
        w.WriteStartObject("params");
        writeParams(w);
        w.WriteEndObject();
    });

    private static string Message(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream))
        {
            w.WriteStartObject();
            w.WriteString("jsonrpc", "2.0");
            write(w);
            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
