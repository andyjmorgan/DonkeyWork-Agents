namespace DonkeyWork.Agents.Orchestrations.Contracts;

public static class NatsSubjects
{
    public const string CommandSubject = "orchestration.execute";
    public const string CommandStream = "wolverine-orchestrations";
    public const string CommandConsumer = "orchestration-executor";

    public const string AudioGenerationSubject = "audio.generate";
    public const string AudioGenerationStream = "wolverine-audio-generation";
    public const string AudioGenerationConsumer = "audio-generation-worker";

    public static string UserStream(Guid userId) => $"executions-{userId}";

    public static string UserSubjectFilter(Guid userId) => $"execution.{userId}.>";

    public static string ExecutionSubject(Guid userId, Guid executionId) => $"execution.{userId}.{executionId}";
}
