namespace ZGF.Gui.Immediate.Tests;

public sealed class AutomationOptionsTests
{
    [Fact]
    public void Without_the_flag_the_app_runs_as_usual()
    {
        Assert.IsType<AutomationLaunch.Normal>(AutomationOptions.Parse(["--light", "notes.md"]));
    }

    [Fact]
    public void The_flag_carries_a_port_and_a_token()
    {
        var launch = AutomationOptions.Parse(["--light", "--zgf-automation", "47100", "s3cr3t-token", "notes.md"]);

        var endpoint = Assert.IsType<AutomationLaunch.Automated>(launch).Options.Endpoint;
        Assert.NotNull(endpoint);
        Assert.Equal(new AutomationPort.Fixed(47100), endpoint.Port);
        Assert.Equal("s3cr3t-token", endpoint.Token);
    }

    [Theory]
    [InlineData("--zgf-automation")]
    [InlineData("--zgf-automation", "47100")]
    [InlineData("--zgf-automation", "0", "token")]
    [InlineData("--zgf-automation", "65536", "token")]
    [InlineData("--zgf-automation", "+80", "token")]
    [InlineData("--zgf-automation", "port", "token")]
    [InlineData("--zgf-automation", "47100", "")]
    [InlineData("--zgf-automation", "47100", "two words")]
    [InlineData("--zgf-automation", "47100", "a/b")]
    [InlineData("--zgf-automation", "47100", ".")]
    [InlineData("--zgf-automation", "47100", "..")]
    [InlineData("--zgf-automation", "0", "token", "relative/port.txt")]
    [InlineData("--zgf-automation", "47100", "tok%20en")]
    [InlineData("--zgf-automation", "47100", "a", "--zgf-automation", "47101", "b")]
    public void A_flag_without_a_usable_port_and_token_is_malformed(params string[] args)
    {
        var malformed = Assert.IsType<AutomationLaunch.Malformed>(AutomationOptions.Parse(args));
        Assert.Contains("--zgf-automation", malformed.Reason);
    }

    [Fact]
    public void Port_zero_picks_a_port_and_reports_it_in_a_file()
    {
        var file = Path.Combine(Path.GetTempPath(), "zgf-port.txt");

        var launch = AutomationOptions.Parse(["--zgf-automation", "0", "tok", file]);

        var endpoint = Assert.IsType<AutomationLaunch.Automated>(launch).Options.Endpoint;
        Assert.NotNull(endpoint);
        Assert.Equal(new AutomationPort.Chosen(file), endpoint.Port);
    }

    [Fact]
    public void In_process_automation_has_no_endpoint()
    {
        Assert.Null(AutomationOptions.InProcess().Endpoint);
    }
}
