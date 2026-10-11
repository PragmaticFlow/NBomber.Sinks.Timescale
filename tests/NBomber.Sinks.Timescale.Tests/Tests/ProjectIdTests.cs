using NBomber.Sinks.Timescale.Tests.Infra;
using Shouldly;

namespace NBomber.Sinks.Timescale.Tests.Tests;

public class ProjectIdTests(EnvContextFixture fixture) : IClassFixture<EnvContextFixture>
{
    [Theory]
    [InlineData("1001", "1001")]              // valid project id is parsed
    [InlineData("", "")]                    // empty project id falls back to -1
    [InlineData(" ", "")]
    public void ParseProjectId_Should_Parse_Value(string projectId, string? expectedProjectId)
    {
        var sink = fixture.CreateTimescaleDbSinkInstance();

        sink.ParseProjectId(projectId).ShouldBe(expectedProjectId);
    }
}
