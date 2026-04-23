namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

public sealed class MoveRecordingToCollectionRequestV1
{
    public Guid? CollectionId { get; init; }
    public int? SequenceNumber { get; init; }
    public string? ChapterTitle { get; init; }
}
