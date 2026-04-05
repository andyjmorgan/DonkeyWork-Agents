using DonkeyWork.Agents.Orchestrations.Contracts;

namespace DonkeyWork.Agents.Orchestrations.Core.Tests;

public class NatsSubjectsTests
{
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _executionId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void UserStream_ReturnsExpectedFormat()
    {
        var result = NatsSubjects.UserStream(_userId);
        Assert.Equal("executions-11111111-1111-1111-1111-111111111111", result);
    }

    [Fact]
    public void UserSubjectFilter_ReturnsWildcardSuffix()
    {
        var result = NatsSubjects.UserSubjectFilter(_userId);
        Assert.Equal("execution.11111111-1111-1111-1111-111111111111.>", result);
    }

    [Fact]
    public void ExecutionSubject_ReturnsThreePartSubject()
    {
        var result = NatsSubjects.ExecutionSubject(_userId, _executionId);
        Assert.Equal("execution.11111111-1111-1111-1111-111111111111.22222222-2222-2222-2222-222222222222", result);
    }

    [Fact]
    public void ExecutionSubject_MatchesUserSubjectFilter()
    {
        var subject = NatsSubjects.ExecutionSubject(_userId, _executionId);
        var filter = NatsSubjects.UserSubjectFilter(_userId);

        var filterPrefix = filter.Replace(".>", ".");
        Assert.StartsWith(filterPrefix, subject);
    }
}
