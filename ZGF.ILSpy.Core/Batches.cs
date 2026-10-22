using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace ZGF.ILSpy.Core;

internal static class Batches
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(50);

    /// <summary>Runs <paramref name="produce"/> on the thread pool and yields what it emits in
    /// batches: all that arrived since the last batch, at most one batch per interval. A failure of
    /// <paramref name="produce"/> ends the stream with it; leaving the stream early cancels it.</summary>
    public static async IAsyncEnumerable<IReadOnlyList<T>> Stream<T>(Action<Action<T>, CancellationToken> produce,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = stop.Token;
        _ = Task.Run(() =>
        {
            try
            {
                produce(item => channel.Writer.TryWrite(item), token);
                channel.Writer.TryComplete();
            }
            catch (Exception e)
            {
                channel.Writer.TryComplete(e);
            }
        }, CancellationToken.None);

        try
        {
            while (await channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var batch = new List<T>();
                while (channel.Reader.TryRead(out var item))
                    batch.Add(item);
                yield return batch;
                if (!channel.Reader.Completion.IsCompleted)
                    await Task.Delay(Interval, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            stop.Cancel();
        }
    }
}
