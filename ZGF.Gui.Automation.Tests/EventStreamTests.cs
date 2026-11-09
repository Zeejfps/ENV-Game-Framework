namespace ZGF.Gui.Automation.Tests;

/// <summary>Reading an MCP answer off an event stream as the format says, without the server closing it.</summary>
public sealed class EventStreamTests
{
    private const string Answer = "{\"jsonrpc\":\"2.0\",\"id\":7,\"result\":{}}";

    private static string Read(string stream) => McpHttpClient.ReadEventStream(new StringReader(stream), 7, CancellationToken.None);

    [Fact]
    public void The_answer_is_the_event_whose_id_matches()
    {
        var stream = "data: {\"jsonrpc\":\"2.0\",\"method\":\"notifications/progress\"}\n\n: a comment\nevent: message\ndata: " + Answer + "\n\n";

        Assert.Equal(Answer, Read(stream));
    }

    [Fact]
    public void An_event_the_stream_ends_inside_is_dropped()
    {
        Assert.Throws<McpProtocolException>(() => Read("data: " + Answer + "\n"));
        Assert.Throws<McpProtocolException>(() => Read("data: " + Answer));
    }

    [Fact]
    public void Only_one_space_after_the_colon_is_dropped()
    {
        Assert.Equal(Answer, Read("data:" + Answer + "\n\n"));
        Assert.Equal(Answer, Read("data: " + Answer + "\n\n"));
        Assert.Equal(" " + Answer, Read("data:  " + Answer + "\n\n"));
    }

    [Fact]
    public void Data_lines_join_with_line_feeds_and_bare_or_empty_data_fields_add_empty_lines()
    {
        var stream = "data: {\"jsonrpc\":\"2.0\",\r\ndata\ndata:\ndata: \"id\":7,\"result\":{}}\r\n\r\n";

        Assert.Equal("{\"jsonrpc\":\"2.0\",\n\n\n\"id\":7,\"result\":{}}", Read(stream));
    }

    [Fact]
    public void An_event_with_only_empty_data_answers_nothing()
    {
        Assert.Equal(Answer, Read("data\n\ndata:\n\ndata: " + Answer + "\n\n"));
    }
}
