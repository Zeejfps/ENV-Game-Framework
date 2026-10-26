using ZGF.ILSpy.Core.Resources;

namespace ZGF.ILSpy.Core;

/// <summary>
/// The icons packages show beside their names: each fetched from the URL its package gives, a few at
/// a time, decoded and shrunk to fit a square. Every call may be made from any thread.
/// </summary>
public sealed class PackageIcons
{
    private const int MaxFetches = 4;

    /// <summary>The largest icon file read; nuget.org takes none larger.</summary>
    private const int MaxBytes = 1024 * 1024;

    private readonly HttpClient _client;
    private readonly SemaphoreSlim _fetches = new(MaxFetches, MaxFetches);

    /// <param name="handler">Sends the requests; the app's shared connections by default.</param>
    public PackageIcons(HttpMessageHandler? handler = null)
    {
        _client = NuGetFeed.ClientFor(handler);
    }

    /// <summary>The picture at <paramref name="url"/>, shrunk to fit a square <paramref name="side"/>
    /// pixels across.</summary>
    /// <exception cref="HttpRequestException">The picture could not be fetched.</exception>
    /// <exception cref="InvalidDataException">It is no picture, or too large a file.</exception>
    public async Task<ResourceImage> FetchAsync(string url, int side, CancellationToken cancellationToken = default)
    {
        await _fetches.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var bytes = await ReadBytesAsync(url, cancellationToken).ConfigureAwait(false);
            return ResourceImage.Decode(bytes).FitInto(side);
        }
        finally
        {
            _fetches.Release();
        }
    }

    private async Task<byte[]> ReadBytesAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > MaxBytes)
            throw new InvalidDataException($"The icon at {url} is larger than {MaxBytes} bytes.");
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (bytes.Length > MaxBytes)
            throw new InvalidDataException($"The icon at {url} is larger than {MaxBytes} bytes.");
        return bytes;
    }
}
