using System.Net;
using System.Text;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

/// <summary>A feed served from memory: each URL answers with the text or bytes put there, any other
/// with 404, and every request is noted.</summary>
internal sealed class FakeFeed : HttpMessageHandler
{
    public const string IndexUrl = "https://feed.test/v3/index.json";
    public const string SearchUrl = "https://feed.test/search";
    public const string PackagesUrl = "https://feed.test/flat/";

    private readonly Dictionary<string, byte[]> _answers = new();
    private int _cancelled;

    public FakeFeed()
    {
        Answer(IndexUrl, $$"""
            { "version": "3.0.0", "resources": [
                { "@id": "{{SearchUrl}}", "@type": "SearchQueryService/3.5.0" },
                { "@id": "{{PackagesUrl}}", "@type": "PackageBaseAddress/3.0.0" } ] }
            """);
    }

    public List<string> Requested { get; } = new();

    /// <summary>Whether requests wait, unanswered, until they are cancelled.</summary>
    public bool Holds;

    /// <summary>How many held requests were cancelled.</summary>
    public int Cancelled => Volatile.Read(ref _cancelled);

    public NuGetSource Source => new("test", IndexUrl);

    public void Answer(string url, string text) => _answers[url] = Encoding.UTF8.GetBytes(text);

    public void Answer(string url, byte[] bytes) => _answers[url] = bytes;

    /// <summary>Answers every search with <paramref name="json"/>.</summary>
    public void AnswerSearches(string json) => Answer(SearchUrl, json);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri!.ToString();
        Requested.Add(url);
        if (Holds)
            await HoldUntilCancelled(cancellationToken);
        var key = StripQuery(url);
        if (!_answers.TryGetValue(key, out var bytes))
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
    }

    private async Task HoldUntilCancelled(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Interlocked.Increment(ref _cancelled);
            throw;
        }
    }

    private static string StripQuery(string url)
    {
        var at = url.IndexOf('?');
        if (at < 0)
            return url;
        return url.Substring(0, at);
    }
}
